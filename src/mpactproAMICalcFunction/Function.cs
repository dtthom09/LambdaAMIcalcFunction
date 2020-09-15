using System;
using System.Collections.Generic;
using System.Linq;
using Amazon.Lambda.Core;
using BrickBridge.Models;
using PodioCore.Utils.ItemFields;
using PodioCore.Items;
using BrickBridge;
using PodioCore.Models;


// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace mpactproAMICalcFunction
{
    public class AMICalcFunction:saasafrasLambdaBaseFunction.Function
    {
        private int GetFieldId(string key)
        {
            var field = _deployedSpaces[key];
            return int.Parse(field);
        }
        private Dictionary<string, string> _deployedSpaces;

        public override async System.Threading.Tasks.Task InnerHandler(RoutedPodioEvent e, ILambdaContext lambda_ctx)
        {
            System.Environment.SetEnvironmentVariable("PODIO_PROXY_URL", Config.PODIO_PROXY_URL);
            System.Environment.SetEnvironmentVariable("BBC_SERVICE_URL", Config.BBC_SERVICE_URL);
            System.Environment.SetEnvironmentVariable("BBC_SERVICE_API_KEY", Config.BBC_SERVICE_API_KEY);

            _deployedSpaces = e.currentEnvironment.deployments.First(a => a.appId == "mpactprobeta").deployedSpaces;
            var factory = new AuditedPodioClientFactory(e.appId, e.version, e.clientId, e.currentEnvironment.environmentId);
            var podioClient = factory.ForClient(e.clientId, e.currentEnvironment.environmentId);
            lambda_ctx.Logger.LogLine($"Podio Routed Event Version and AppId: {e.version}, {e.appId}");
            lambda_ctx.Logger.LogLine($"Lambda_ctx: {lambda_ctx.Identity}");

            string url = Config.LOCKER_URL;
            string key = Config.BBC_SERVICE_API_KEY;
            lambda_ctx.Logger.LogLine($"URL: {url}");
            var functionName = "mpactproAMICalcFunction";
            var uniqueId = e.currentItem.ItemId.ToString();
            var client = new BbcServiceClient(url, key);
            var lockValue = await client.LockFunction(functionName, uniqueId);
            if (string.IsNullOrEmpty(lockValue))
            {
                lambda_ctx.Logger.LogLine($"Failed to acquire lock for {functionName} and id {uniqueId}");
                return;
            }
            try
            {
                var app = e.currentItem.App.Name; //Podio App Name
                Item item = new Item { ItemId = e.currentItem.ItemId };
                double? incomeLimit = 0;
                var householdIncomeBandField = e.currentItem.Field<CategoryItemField>(0);
                var incomeLevel = e.currentItem.Field<CategoryItemField>(0);
                var incomeCategory = e.currentItem.Field<CategoryItemField>(0);
                var size = e.currentItem.Field<NumericItemField>(0);
                var ami = e.currentItem.Field<NumericItemField>(0);
                var income = e.currentItem.Field<MoneyItemField>(0);

                var fieldId = 0;
                var workspace = "";
                lambda_ctx.Logger.LogLine("Checking for app");
                lambda_ctx.Logger.LogLine($"App: {app}");
                
                if(app=="Intake")
                {
                    
                    lambda_ctx.Logger.LogLine("App is Intake");
                    var items=await podioClient.GetReferringItems(e.currentItem.ItemId);
                    lambda_ctx.Logger.LogLine($"Referring Items: {items.Count()}");
                    e.currentItem = await podioClient.GetItem(items.First().Items.First().ItemId);
                    item = new Item { ItemId = e.currentItem.ItemId };
                    lambda_ctx.Logger.LogLine($"Found MCP with item ID: {item.ItemId}");
                    fieldId = GetFieldId("1. Client Profile|Master Client Profile|Household Income Band");
                    householdIncomeBandField = item.Field<CategoryItemField>(fieldId);
                    fieldId = GetFieldId("1. Client Profile|Master Client Profile|Income Levels");
                    incomeLevel = item.Field<CategoryItemField>(fieldId);
                    workspace = "1. Client Profile|Master Client Profile";
                    fieldId = GetFieldId("1. Client Profile|Master Client Profile|Household Size");
                    size = e.currentItem.Field<NumericItemField>(fieldId);
                    fieldId = GetFieldId("1. Client Profile|Master Client Profile|Annual Family or Household Income");
                    income = e.currentItem.Field<MoneyItemField>(fieldId);
                    fieldId = GetFieldId("1. Client Profile|Master Client Profile|County 100% Income Limit (AMI)");
                    ami = e.currentItem.Field<NumericItemField>(fieldId);
                }
                if (app == "Master Client Profile")
                {
                    
                    fieldId = GetFieldId("1. Client Profile|Master Client Profile|Household Income Band");
                    householdIncomeBandField = item.Field<CategoryItemField>(fieldId);
                    fieldId = GetFieldId("1. Client Profile|Master Client Profile|Income Levels");
                    incomeLevel = item.Field<CategoryItemField>(fieldId);
                    workspace = "1. Client Profile|Master Client Profile";
                    fieldId = GetFieldId("1. Client Profile|Master Client Profile|Household Size");
                    size = e.currentItem.Field<NumericItemField>(fieldId);
                    fieldId = GetFieldId("1. Client Profile|Master Client Profile|Annual Family or Household Income");
                    income = e.currentItem.Field<MoneyItemField>(fieldId);
                    fieldId = GetFieldId("1. Client Profile|Master Client Profile|County 100% Income Limit (AMI)");
                    ami = e.currentItem.Field<NumericItemField>(fieldId);
                }

                else if (app == "Home Purchase Case")
                {
                    fieldId = GetFieldId("3. Home Purchase|Home Purchase Case|Household Income Band");
                    householdIncomeBandField = item.Field<CategoryItemField>(fieldId);
                    workspace = "3. Home Purchase|Home Purchase Case";
                    fieldId = GetFieldId("3. Home Purchase|Home Purchase Case|Household Family Size");
                    size = e.currentItem.Field<NumericItemField>(fieldId);
                    fieldId = GetFieldId("3. Home Purchase|Home Purchase Case|Household Annual Family Income");
                    income = e.currentItem.Field<MoneyItemField>(fieldId);
                    fieldId = GetFieldId("3. Home Purchase|Home Purchase Case|County 100% Income Limit (AMI)");
                    ami = e.currentItem.Field<NumericItemField>(fieldId);
                }

                else if (app == "Homeowner Services Case")
                {
                    fieldId = GetFieldId("7. Home Owner Services|Homeowner Services Case|Household Income Band");
                    householdIncomeBandField = item.Field<CategoryItemField>(fieldId);
                    workspace = "7. Home Owner Services|Homeowner Services Case";
                    fieldId = GetFieldId("7. Home Owner Services|Homeowner Services Case|Household Family Size");
                    size = e.currentItem.Field<NumericItemField>(fieldId);
                    fieldId = GetFieldId("7. Home Owner Services|Homeowner Services Case|Household Annual Family Income");
                    income = e.currentItem.Field<MoneyItemField>(fieldId);
                    fieldId = GetFieldId("7. Home Owner Services|Homeowner Services Case|County 100% Income Limit (AMI)");
                    ami = e.currentItem.Field<NumericItemField>(fieldId);
                }

                else if (app == "Mortgage Modification Case")
                {
                    fieldId = GetFieldId("4. Mortgage Modification|Mortgage Modification Case|Household Income Band");
                    householdIncomeBandField = item.Field<CategoryItemField>(fieldId);
                    fieldId = GetFieldId("4. Mortgage Modification|Mortgage Modification Case|Income Level");
                    incomeLevel = item.Field<CategoryItemField>(fieldId);
                    fieldId = GetFieldId("4. Mortgage Modification|Mortgage Modification Case|Income Category");
                    incomeCategory = item.Field<CategoryItemField>(fieldId);
                    workspace = "4. Mortgage Modification|Mortgage Modification Case";
                    fieldId = GetFieldId("4. Mortgage Modification|Mortgage Modification Case|Household Family Size");
                    size = e.currentItem.Field<NumericItemField>(fieldId);
                    fieldId = GetFieldId("4. Mortgage Modification|Mortgage Modification Case|Household Annual Family Income");
                    income = e.currentItem.Field<MoneyItemField>(fieldId);
                    fieldId = GetFieldId("4. Mortgage Modification|Mortgage Modification Case|County 100% Income Limit (AMI)");
                    ami = e.currentItem.Field<NumericItemField>(fieldId);
                }

                lambda_ctx.Logger.LogLine($"Destination {workspace}");
                lambda_ctx.Logger.LogLine("If Statement Starting");

                if (ami.Value != null && size.Value != null && income.Value != null)
                {
                    if (e.currentItem.Revisions != null)
                    {
                        var revision = await podioClient.GetRevisionDifference(Convert.ToInt32(e.currentItem.ItemId), e.currentItem.CurrentRevision.Revision - 1, e.currentItem.CurrentRevision.Revision);
                        lambda_ctx.Logger.LogLine("Passed if statement");
                        if (revision.Count > 0 && revision.First().FieldId != ami.FieldId && revision.First().FieldId != size.FieldId && revision.First().FieldId != income.FieldId)
                            return;
                    }
                }
                lambda_ctx.Logger.LogLine("Calculating incomeLimit");
                lambda_ctx.Logger.LogLine($"AMI: {ami.Value}");
                lambda_ctx.Logger.LogLine($"Size: {size.Value}");
                lambda_ctx.Logger.LogLine($"Income: {income.Value}");
                if (size.Value > 0)
                    {
                    var medianIncome = ami.Value / 2;

                    if (size.Value > 4)
                    {
                        var overNumber = size.Value - 4;
                        var overOffSet = overNumber * 0.08;
                        var offSet = medianIncome * (1 + overOffSet);
                        ami.Value = offSet * 2;
                        incomeLimit = Convert.ToDouble(income.Value) / ami.Value ;
                    }
                   else if (size.Value < 4)
                    {
                        var underNumber = size.Value - 4;
                        var underOffSet = underNumber * 0.1;
                        var offSet = medianIncome * (1 + underOffSet);
                        ami.Value = offSet * 2;
                        incomeLimit = incomeLimit = Convert.ToDouble(income.Value) / ami.Value;
                        
                    }
                    else if (size.Value == 4)
                    {
                        incomeLimit = incomeLimit = Convert.ToDouble(income.Value) / ami.Value;
                    }
                }

                //gets household income band to set and income limit based on app name
                lambda_ctx.Logger.LogLine($"calculated value: {incomeLimit}");
                //sets household income band based on income limit
                lambda_ctx.Logger.LogLine("Setting values");
                if (incomeLimit < .5)
                    incomeCategory.OptionText = "A - Less than 50% of AMI";

                if (incomeLimit > 0 && incomeLimit < .3)
                {
                    householdIncomeBandField.OptionText = "1. Below 30% of AMI";
                    incomeLevel.OptionText = "a. < 30% of Area Median Income (AMI)";
                }
                else if (incomeLimit >= .3 && incomeLimit < .5)
                {
                    householdIncomeBandField.OptionText = "2. 30% - 49% of AMI";
                    incomeLevel.OptionText = "b . 30 - 49% of AMI";
                }
                else if (incomeLimit >= .5 && incomeLimit < .8)
                {
                    householdIncomeBandField.OptionText = "3. 50% - 79% of AMI";
                    incomeLevel.OptionText = "c. 50 - 79% of AMI";
                    incomeCategory.OptionText = "B - 50%-79% AMI";
                }
                else if (incomeLimit >= .8 && incomeLimit <= 1)
                {
                    householdIncomeBandField.OptionText = "4. 80% - 100% of AMI";
                    incomeLevel.OptionText = "d. 80 - 100% of AMI";
                    incomeCategory.OptionText = "C - 80-100% AMI";
                }
                else if (incomeLimit > 1)
                {
                    householdIncomeBandField.OptionText = "5. 101% - 120% of AMI";
                    incomeLevel.OptionText = "e. > 100% AMI";
                    incomeCategory.OptionText = "D - Greater than 100% AMI";
                }
                else
                {
                    householdIncomeBandField.OptionText = "f. Chose not to respond";
                    incomeLevel.OptionText = "f. Chose not to respond";
                }
                await podioClient.UpdateItem(item, false);
            }
            catch (Exception ex)
            {
                lambda_ctx.Logger.LogLine($"Exception: {ex}");
            }
            finally
            {
                await client.UnlockFunction(functionName, uniqueId, lockValue);
            }
        }
    }
}