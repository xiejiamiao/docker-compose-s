using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CSRedis;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using StackExchange.Redis;

// ReSharper disable StringLiteralTypo

namespace Jiamiao.x.RedisConnectSample.Controllers
{
    [ApiController]
    [Route("[controller]/[action]")]
    public class RedisController : Controller
    {
        private readonly ILogger<RedisController> _logger;
        private readonly IConfiguration _configuration;
        private readonly CsRedisCoreSentinel _csRedis;

        public RedisController(ILogger<RedisController> logger,IConfiguration configuration,CsRedisCoreSentinel csRedis)
        {
            _logger = logger;
            _configuration = configuration;
            _csRedis = csRedis;
        }

        [HttpGet]
        public async Task<string> Get()
        {
            var msg = new StringBuilder();

            var stopWatch = new Stopwatch();
            stopWatch.Start();
            var csRedisValue = await UseCsRedisCore();
            stopWatch.Stop();
            msg.AppendLine($"CSRedisCore -> 获取到值{csRedisValue}  耗时{stopWatch.ElapsedMilliseconds}毫秒");
            _logger.LogInformation($"CSRedisCore -> 获取到值{csRedisValue}  耗时{stopWatch.ElapsedMilliseconds}毫秒");

            stopWatch.Reset();
            stopWatch.Start();
            var stackExchangeRedisValue = await UseStackExchangeRedis();
            stopWatch.Stop();
            msg.AppendLine($"StackExchange -> 获取到值{stackExchangeRedisValue}  耗时{stopWatch.ElapsedMilliseconds}毫秒");
            _logger.LogInformation($"StackExchange -> 获取到值{stackExchangeRedisValue}  耗时{stopWatch.ElapsedMilliseconds}毫秒");
            
            return msg.ToString();
        }

        [HttpGet]
        public async Task<IActionResult> UseCsRedisCore()
        {
            var stopWatch = new Stopwatch();
            stopWatch.Start();
            await Task.CompletedTask;
            var strRedisEndPoints = _configuration["DIMSUM_REDISENDPOINT"];
            var redisEndPoints = strRedisEndPoints.Split('|').ToArray();
            _logger.LogInformation($"尝试连接Redis:{JsonConvert.SerializeObject(redisEndPoints)}");
            var csRedis = new CSRedisClient($"{_configuration["DIMSUM_MASTERSERVICENAME"]},password={_configuration["DIMSUM_REDISPWD"]}", redisEndPoints);
            RedisHelper.Initialization(csRedis);

            var value = csRedis.Get("username");
            stopWatch.Stop();
            return Json(new {key = "username", value = value, spendTime = stopWatch.ElapsedMilliseconds});
        }

        [HttpGet]
        public async Task<IActionResult> CSRedisCore(string key,string value)
        {
            await Task.CompletedTask;
            var stopWatch = new Stopwatch();
            stopWatch.Start();

            var writer = await _csRedis.Writer();
            var writeResult = writer.Set(key, value);

            stopWatch.Stop();
            _logger.LogInformation($"写入操作完成，结果：{writeResult}   耗时:{stopWatch.ElapsedMilliseconds}");
            stopWatch.Reset();

            stopWatch.Start();
            var reader = await _csRedis.Reader();
            
            var dbValue = reader.Get(key);
            stopWatch.Stop();
            _logger.LogInformation($"读取操作完成，结果：{dbValue}   耗时:{stopWatch.ElapsedMilliseconds}");

            stopWatch.Reset();

            return Json(new { });
        }

        [HttpGet]
        public async Task<IActionResult> UseStackExchangeRedis()
        {
            var stopWatch = new Stopwatch();
            stopWatch.Start();

            var strRedisEndPoints = _configuration["DIMSUM_REDISENDPOINT"];
            var redisEndPoints = strRedisEndPoints.Split('|').Select(x => new {url = x.Split(':')[0], port = int.Parse(x.Split(':')[1])});
            var sentinelOptions = new ConfigurationOptions()
            {
                TieBreaker = "",
                CommandMap = CommandMap.Sentinel,
                AbortOnConnectFail = false
            };
            foreach (var redisEndPoint in redisEndPoints)
            {
                sentinelOptions.EndPoints.Add(redisEndPoint.url,redisEndPoint.port);
            }

            var sentinelConnection = await ConnectionMultiplexer.ConnectAsync(sentinelOptions);
            _logger.LogInformation($"sentinelConnection.IsConnected:{sentinelConnection.IsConnected}   sentinelConnection.IsConnecting:{sentinelConnection.IsConnecting}");

            var redisServiceOptions = new ConfigurationOptions()
            {
                ServiceName = _configuration["DIMSUM_MASTERSERVICENAME"],
                Password = _configuration["DIMSUM_REDISPWD"],
                AbortOnConnectFail = true
            };
            _logger.LogInformation(JsonConvert.SerializeObject(new
            {
                EndPoints = sentinelOptions.EndPoints, 
                ServiceName = redisServiceOptions.ServiceName,
                Password = redisServiceOptions.Password
            }));

            var masterConnection = sentinelConnection.GetSentinelMasterConnection(redisServiceOptions);

            var db = masterConnection.GetDatabase();
            var value = db.StringGet("username");

            stopWatch.Stop();
            return Json(new { key = "username", value = value, spendTime = stopWatch.ElapsedMilliseconds });
        }
    }
}
