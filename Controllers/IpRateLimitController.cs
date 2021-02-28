using AspNetCoreRateLimit;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebAPIVersionDemo.Controllers
{
    /// <summary>
    /// IP限制控制器
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class IpRateLimitController : ControllerBase
    {

        private readonly IpRateLimitOptions _options;
        private readonly IIpPolicyStore _ipPolicyStore;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="optionsAccessor"></param>
        /// <param name="ipPolicyStore"></param>
        public IpRateLimitController(IOptions<IpRateLimitOptions> optionsAccessor, IIpPolicyStore ipPolicyStore)
        {
            _options = optionsAccessor.Value;
            _ipPolicyStore = ipPolicyStore;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public async Task<IpRateLimitPolicies> Get()
        {
            var result = await _ipPolicyStore.GetAsync(_options.IpPolicyPrefix);
            return result;
        }

        /// <summary>
        /// 
        /// </summary>
        [HttpPost]
        public async Task Post(IpRateLimitPolicy ipRate)
        {
            var pol = await _ipPolicyStore.GetAsync(_options.IpPolicyPrefix);
            if (ipRate != null)
            {
                pol.IpRules.Add(new IpRateLimitPolicy
                {
                    Ip = "8.8.4.4",
                    Rules = new List<RateLimitRule>(new RateLimitRule[] {
                    new RateLimitRule {Endpoint = "*:/api/testupdate",Limit = 100,Period = "1d" }
                })
                });
                pol.IpRules.Add(ipRate);
                await _ipPolicyStore.SetAsync(_options.IpPolicyPrefix, pol);
            }
        }
    }
}
