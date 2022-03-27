using LannisterPay.Models;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using Newtonsoft.Json;

namespace LannisterPay.Services
{
    public class PaymentProcessorService:IPaymentProcessorService
    {
        private readonly AppConfig _appConfig;
        private readonly string[] _supportedEntityTypes;
        //private readonly IMongoDatabase _database;
        private readonly IMongoCollection<FeeConfig> _feeConfigCollection;
        public PaymentProcessorService(IOptions<AppConfig> options)
        {
            _appConfig =  options.Value;
            _supportedEntityTypes = new string[]{ "CREDIT-CARD", "DEBIT-CARD", "BANK-ACCOUNT", "USSD", "WALLET-ID"};
            var client =  new MongoClient(_appConfig.MongoDbConString);
            var db = client.GetDatabase("LannisterPayDB");
            _feeConfigCollection = db.GetCollection<FeeConfig>("feeConfig");


        }
        private async Task<bool> CreateFeeConfig(FeeConfig feeConfig)
        {
            var result = false;
            
            await _feeConfigCollection.InsertOneAsync(feeConfig);

            return result;

        }

        private async Task<bool> CreateFeeConfig(List<FeeConfig> feeConfigs)
        {
            var result = false;
          
            
            await _feeConfigCollection.InsertManyAsync(feeConfigs);
            result = true;

            return result;

        }

        public async Task<bool> ProcessFeeConfiguationSetup(FeeConfigSetupRequest request)
        {
            var result = false;
            //get all lines in the string
            //var allConfigs = request.FeeConfigurationSpec.Split(new String[] { Environment.NewLine }, StringSplitOptions.None);
            var allConfigs = request.FeeConfigurationSpec.Split("\n");
            var feeConfigs = new List<FeeConfig>();
            foreach (var config in allConfigs)
            {
                var ffConfig = new FeeConfig();
                var spConfig =config.Split(" ");

                ffConfig.FeeId = spConfig[0].Trim(); //config.Trim().Substring(0, 8);
                ///check if this exists
                var f = await GetFeeConfig(ffConfig.FeeId);
                if(f != null)
                {
                    //this already exists
                    continue;
                }
                var remString = config.Substring(8).Trim();
                ffConfig.FeeCurrency = spConfig[1].Trim();// remString.Substring(0, remString.IndexOf(' ')).Trim();
                //remString = remString.Substring(remString.IndexOf(' ')).Trim();
                ffConfig.FeeLocale = spConfig[2].Trim(); //remString.Substring(0,remString.IndexOf(' ')).Trim();
                var entity = spConfig[3].Trim().Substring(0, spConfig[3].Trim().IndexOf("("));
                if (!_supportedEntityTypes.Contains(entity) && entity != "*")
                {
                    //this entity type value is not supported. skip it and go to the newxt record
                    continue;
                }
                ffConfig.FeeEntity = entity;
                
                ffConfig.EntityProperty = spConfig[3].Trim().Substring(spConfig[3].Trim().IndexOf("(") + 1);
                ffConfig.EntityProperty = ffConfig.EntityProperty.Replace(")","");
                ffConfig.FeeType = spConfig[6].Trim();
                ffConfig.FeeValue = spConfig[7].Trim();
                ffConfig =  ComputeFeePrecedence(ffConfig);

                feeConfigs.Add(ffConfig);
                //var kdd = await CreateFeeConfig(ffConfig);

            }
            //save config to mongo db
            if (feeConfigs.Count == 0)
                return false;

            return await CreateFeeConfig(feeConfigs);
        }

        private FeeConfig ComputeFeePrecedence(FeeConfig feeConfig)
        {
            if(feeConfig.FeeLocale == "*" && feeConfig.FeeEntity == "*" && feeConfig.EntityProperty == "*")
            {
                feeConfig.FeePriority = 4;

            }
            else if(feeConfig.FeeLocale != "*" && feeConfig.FeeEntity != "*" && feeConfig.EntityProperty != "*")
            {
                feeConfig.FeePriority = 1;
            }
            else if ((feeConfig.FeeLocale == "*" && feeConfig.FeeEntity == "*" && feeConfig.EntityProperty != "*") ||
                (feeConfig.FeeLocale == "*" && feeConfig.FeeEntity != "*" && feeConfig.EntityProperty == "*") ||
                (feeConfig.FeeLocale != "*" && feeConfig.FeeEntity == "*" && feeConfig.EntityProperty == "*")
                )
            {
                feeConfig.FeePriority = 3;
            }
            else if ((feeConfig.FeeLocale == "*" && feeConfig.FeeEntity != "*" && feeConfig.EntityProperty != "*") ||
               (feeConfig.FeeLocale != "*" && feeConfig.FeeEntity == "*" && feeConfig.EntityProperty != "*") ||
               (feeConfig.FeeLocale != "*" && feeConfig.FeeEntity != "*" && feeConfig.EntityProperty == "*")
               )
            {
                feeConfig.FeePriority = 2;
            }
            return feeConfig;

        }

        private async Task<FeeConfig> GetFeeConfig(string Id)
        {
            
            
            //check if a fee with this id exists
           
            try
            {
                
                return await _feeConfigCollection.Find(x=> x.FeeId == Id).FirstOrDefaultAsync();
                
                
            }
            catch (Exception e)
            {

                return null;
            }
        }
        private async Task<List<FeeConfig>> GetFeeConfigs()
        {


            //check if a fee with this id exists

            try
            {

                return  await _feeConfigCollection.Find(_=> true).ToListAsync();


            }
            catch (Exception e)
            {

                return null;
            }
        }

        
        private async Task<FeeConfig> GetFeeConfig(string locale, string currency, PaymentEntityDetails entity )
        {


            //check if a fee with this id exists

            try
            {
                //ID, Issuer, Brand, Number and SixID   
                return await _feeConfigCollection.Find(x => (x.FeeLocale == locale || x.FeeLocale == "*") && x.FeeCurrency == currency && (x.FeeEntity == entity.Type || x.FeeEntity == "*") && (x.EntityProperty == entity.Id.ToString() || x.EntityProperty == "*" || x.EntityProperty == entity.Issuer || x.EntityProperty == entity.Brand || x.EntityProperty == entity.Number || x.EntityProperty ==  entity.SixID)).SortBy(x=> x.FeePriority).FirstOrDefaultAsync();


            }
            catch (Exception e)
            {

                return null;
            }
        }

        public async Task<TransactionResponse> ComputeTransactionFee(Transaction transaction)
        {
            TransactionResponse response = null;
            var locale = string.Empty;
            if(transaction.CurrencyCountry == transaction.PaymentEntity.Country)
            {
                locale = "LOCL";
            }
            else
            {
                locale = "INTL";
            }
            var feeConfig = await GetFeeConfig(locale, transaction.Currency, transaction.PaymentEntity);
            if(feeConfig != null)
            {
                response = new TransactionResponse { AppliedFeeID  = feeConfig.FeeId};
                if(feeConfig.FeeType == "FLAT")
                {
                    response.AppliedFeeValue = Convert.ToDecimal(feeConfig.FeeValue);

                }else if(feeConfig.FeeType == "PERC")
                {
                    response.AppliedFeeValue = Convert.ToDecimal(feeConfig.FeeValue) * transaction.Amount / 100M;
                }else if(feeConfig.FeeType== "FLAT_PERC")
                {
                    var rates = feeConfig.FeeValue.Split(":");
                    if (rates == null || rates.Length == 0)
                        throw new Exception("No fee setup found for this transaction ");


                    response.AppliedFeeValue = Convert.ToDecimal(rates[0]) + ( Convert.ToDecimal(rates[1]) * transaction.Amount / 100M);
                }
                response.ChargeAmount = transaction.Customer.BearsFee ? transaction.Amount +  response.AppliedFeeValue : transaction.Amount;
                response.SettlementAmount = response.ChargeAmount - response.AppliedFeeValue;
            }
            else
            {
                throw new Exception("No fee setup found for this transaction");
            }
            return response;
        }
    }

    
    public interface IPaymentProcessorService
    {
        Task<TransactionResponse> ComputeTransactionFee(Transaction transaction);
        Task<bool> ProcessFeeConfiguationSetup(FeeConfigSetupRequest request);
    }
}
