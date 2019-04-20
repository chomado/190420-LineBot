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
using Microsoft.Bot.Configuration;
using Microsoft.Bot.Builder;
using Microsoft.Azure.CognitiveServices.Knowledge.QnAMaker;
using System.Net.Http;
using Newtonsoft.Json;
using System.Text;
using System.Net.Http.Headers;
using EchoBot.Models;

namespace EchoBot.Controllers
{
    [Route("api/clova")]
    public class ClovaController : Controller
    {
        private readonly ClovaClient _client;
        private readonly IConfiguration _configuration;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger _logger;

        public ClovaController(IConfiguration configuration, ILoggerFactory loggerFactory, IHttpClientFactory httpClientFactory)
        {
            _client = new ClovaClient();
            _configuration = configuration;
            _httpClientFactory = httpClientFactory;
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
                                        response.AddText(text: $"{food}の話ですね？");
                                        
                                        // QnA から答えを取り出す
                                        var client = _httpClientFactory.CreateClient();
                                        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("EndpointKey", _configuration.GetValue<string>("EndpointKey"));
                                        var content = new StringContent(JsonConvert.SerializeObject(new { question = food }), Encoding.UTF8, "application/json");
                                        var qnaMakerResponse = await client.PostAsync(
                                            $"{_configuration.GetValue<string>("Hostname")}/knowledgebases/{_configuration.GetValue<string>("KbId")}/generateAnswer",
                                            content);
                                        qnaMakerResponse.EnsureSuccessStatusCode();
                                        var answers = await qnaMakerResponse.Content.ReadAsAsync<QnAMakerResponse>();

                                        // LINE messaging に push
                                        string accessToken = _configuration.GetValue<string>("LINEMessaginAPIKey");
                                        string myUserId = _configuration.GetValue<string>("myUserID");

                                        var answer = answers.Answers?.FirstOrDefault()?.AnswerText;
                                        if (!string.IsNullOrEmpty(answer))
                                        {
                                            response.AddText(answer.Contains("OK") ? $"{food}は食べられます。詳細を LINE に送ります。" : $"{food}は食べられません。");
                                            var messagingClient = new LineMessagingClient(channelAccessToken: accessToken);
                                                await messagingClient.PushMessageAsync(
                                                    // to: request.Session.User.UserId,
                                                    to: myUserId,
                                                    messages: new List<ISendMessage>
                                                    {
                                                    new TextMessage($"{food}"),
                                                    new TextMessage($"解説はこれだよ"),
                                                    new TextMessage(answer),
                                                    }
                                                );
                                        }
                                        else
                                        {
                                            response.AddText($"{food}が見つかりませんでした。");
                                        }
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
