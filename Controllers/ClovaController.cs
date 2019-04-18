using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using CEK.CSharp.Models;
using CEK.CSharp;
using Line.Messaging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace EchoBot.Controllers
{
    [Route("api/clova")]
    public class ClovaController : Controller
    {
        private ClovaClient _client;
        private IConfiguration _configuration;
        private readonly ILogger _logger;

        public ClovaController(IConfiguration configuration, ILoggerFactory loggerFactory)
        {
            _client = new ClovaClient();
            _configuration = configuration;
            _logger = loggerFactory.CreateLogger<ClovaController>();
        }

        [HttpPost]
        public async Task<IActionResult> Post()
        {
            CEKRequest request = await _client.GetRequest(
                Request.Headers["SignatureCEK"],
                Request.Body
            );

            var response = new CEKResponse();

            try
            {
                switch (request.Request.Type)
                {
                    case RequestType.LaunchRequest:
                        {
                            response.AddText("こんにちは！これ食べれるニャンボットです。人間の食べ物を入力すると、これは猫が食べて大丈夫かどうかを答えるよ");
                            response.ShouldEndSession = false;
                            break;
                        }
                    case RequestType.SessionEndedRequest:
                        {
                            response.AddText("またね～");
                            break;
                        }
                    case RequestType.IntentRequest:
                        {
                            switch (request.Request.Intent.Name)
                            {
                                case "HelloIntent":
                                    {
                                        response.AddText("こんにちは！これ食べれるニャンボットだよ！");
                                        response.ShouldEndSession = false;
                                        break;
                                    }
                                case "ChomadoIntent": // "ちょまど"
                                    {
                                        response.AddText("ちょまどさん、さすがオトナです！素敵！すばらしい！松屋！");
                                        response.ShouldEndSession = false;
                                        break;
                                    }
                                case "Clova.GuideIntent":
                                    {
                                        response.AddText("パン、と言ってみてください。");
                                        response.ShouldEndSession = false;
                                        break;
                                    }
                                case "CatIntent": // "パン"
                                    {
                                        var food = request.Request.Intent.Slots.FirstOrDefault().Value.Value;
                                        // 食べれるものが渡ってくるので何か処理をする TODO!
                                        response.AddText(text: $"{food}の話ですか？");
                                        // 「食べられるよ」/「食べられないよ」

                                        // TODO: QnA で分からない時の処理を書く
                                        

                                        // LINE messaging に push
                                        string accessToken = _configuration.GetValue<string>("LINEMessaginAPIKey");
                                        string myUserId = _configuration.GetValue<string>("myUserID");

                                        _logger.LogInformation($"{accessToken}: {myUserId}");

                                        var messagingClient = new LineMessagingClient(channelAccessToken: accessToken);
                                        await messagingClient.PushMessageAsync(
                                            // to: request.Session.User.UserId,
                                            to: myUserId,
                                            messages: new List<ISendMessage>
                                            {
                                                new TextMessage($"{food}"),
                                                new TextMessage($"解説はこれだよ"),
                                            }
                                        );

                                        response.ShouldEndSession = false;
                                        break;
                                    }
                            }
                            break;
                        }
                }
            }
            catch (Exception ex)
            {
                response.AddText(ex.ToString());
            }

            return new OkObjectResult(response);
        }
    }
}