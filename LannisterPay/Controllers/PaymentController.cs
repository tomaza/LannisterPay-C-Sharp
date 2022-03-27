using LannisterPay.Models;
using LannisterPay.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace LannisterPay.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PaymentController : ControllerBase
    {
        private readonly IPaymentProcessorService _paymentProcessorService;
        public PaymentController(IPaymentProcessorService paymentProcessorService)
        {
            _paymentProcessorService = paymentProcessorService;
        }
        [HttpPost("fees")]
        public async Task<IActionResult> Fees([FromBody] FeeConfigSetupRequest request)
        {
            try
            {
                var result = await _paymentProcessorService.ProcessFeeConfiguationSetup(request);
                if (result)
                    return Ok(new { status = "ok" });
                else
                    return Ok(new { status = "failed" });
            }
            catch(Exception ex)
            {
                return BadRequest("something went wrong");
            }
        }

        [HttpPost("compute-transaction-fee")]
        public async Task<IActionResult> ComputeTransactionFee([FromBody] Transaction request)
        {
            try
            {
                
                return Ok(await _paymentProcessorService.ComputeTransactionFee(request));
            }
            catch (Exception ex)
            {
                return BadRequest("Error encountered while computing the transaction fee: "+ex.Message);
            }
        }
    }
}
