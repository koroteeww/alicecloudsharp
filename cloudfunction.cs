using System;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using System.Net.Http;
using System.Net;
using System.Net.Http.Headers;
using System.Web;

namespace Function
{
    public class AliceRequestBase
    {


        [JsonPropertyName("meta")]
        public AliceMetaModel Meta { get; set; }

        [JsonPropertyName("session")]
        public AliceSessionModel Session { get; set; }

        [JsonPropertyName("request")]
        public AliceRequestModel Request { get; set; }

        [JsonPropertyName("version")]
        public string Version { get; set; }
    }
    public class AliceMetaModel
    {
        [JsonPropertyName("locale")]
        public string Locale { get; set; }

        [JsonPropertyName("timezone")]
        public string Timezone { get; set; }

        [JsonPropertyName("client_id")]
        public string ClientId { get; set; }

        [JsonPropertyName("interfaces")]
        public AliceInterfacesModel Interfaces { get; set; }
    }

    public class AliceInterfacesModel
    {
        [JsonPropertyName("screen")]
        public object Screen { get; set; }

        [JsonPropertyName("payments")]
        public object Payments { get; set; }

        [JsonPropertyName("account_linking")]
        public object AccountLinking { get; set; }


    }

    public class AliceSessionModel
    {
        [JsonPropertyName("new")]
        public bool New { get; set; }

        [JsonPropertyName("session_id")]
        public string SessionId { get; set; }

        [JsonPropertyName("message_id")]
        public int MessageId { get; set; }

        [JsonPropertyName("skill_id")]
        public string SkillId { get; set; }

        [JsonPropertyName("user_id")]
        public string UserId { get; set; }

        [JsonPropertyName("user")]
        public AliceSessionUserModel User { get; set; }

        [JsonPropertyName("application")]
        public AliceSessionApplicationModel Application { get; set; }


    }
    public class AliceSessionUserModel
    {
        [JsonPropertyName("user_id")]
        public string UserId { get; set; }

        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; }
    }
    public class AliceSessionApplicationModel
    {
        [JsonPropertyName("application_id")]
        public string ApplicationId { get; set; }
    }

    public class AliceRequestModel
    {
        [JsonPropertyName("command")]
        public string Command { get; set; }

        [JsonPropertyName("original_utterance")]
        public string OriginalUtterance { get; set; }

        [JsonPropertyName("payload")]
        public object Payload { get; set; }

        [JsonPropertyName("markup")]
        public AliceMarkupModel Markup { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; }

        //nlu
        [JsonPropertyName("nlu")]
        public AliceNluModel Nlu { get; set; }
    }
    public class AliceMarkupModel
    {
        [JsonPropertyName("dangerous_context")]
        public bool DangerousContext { get; set; }
    }
    //nlu
    public class AliceNluModel
    {
        [JsonPropertyName("tokens")]
        public IEnumerable<string> Tokens { get; set; }

        [JsonPropertyName("entities")]
        public IEnumerable<NluEntity> Entities { get; set; }

        [JsonPropertyName("intents")]
        public object Intents { get; set; }
    }
    public class NluEntity
    {
        [JsonPropertyName("type")]
        public string type { get; set; }

        [JsonPropertyName("tokens")]
        public NluTokens tokens { get; set; }
        //commented to avoid bugs with FIO
        //[JsonPropertyName("value")]
        //[JsonConverter(typeof(NluValueConverter))]
        //public NluValue value { get; set; }
    }
    public class NluTokens
    {
        [JsonPropertyName("start")]
        public int start { get; set; }
        [JsonPropertyName("end")]
        public int end { get; set; }

    }
    #region NLU
    public class NluValueConverter : EnumerableConverter<NluValue>
    {
        protected override NluValue ToItem(ref Utf8JsonReader reader, JsonSerializerOptions options)
        {
            try 
            {
            return AliceEntityModelConverterHelper.ToItem(ref reader, options);
            }
            catch { return new NluValue();}
        }

        protected override void WriteItem(Utf8JsonWriter writer, NluValue item, JsonSerializerOptions options)
        {
            ConverterHelper.WriteItem(writer, item, options);
        }
    }
    public abstract class EnumerableConverter<TItem> : JsonConverter<IEnumerable<TItem>>
    {
        public override IEnumerable<TItem> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.StartArray:
                    var list = new List<TItem>();
                    while (reader.Read())
                    {
                        if (reader.TokenType == JsonTokenType.EndArray)
                        {
                            break;
                        }

                        list.Add(ToItem(ref reader, options));
                    }

                    return list.ToArray();
                case JsonTokenType.None:
                case JsonTokenType.StartObject:
                case JsonTokenType.EndObject:
                case JsonTokenType.EndArray:
                case JsonTokenType.PropertyName:
                case JsonTokenType.Comment:
                case JsonTokenType.String:
                case JsonTokenType.Number:
                case JsonTokenType.True:
                case JsonTokenType.False:
                case JsonTokenType.Null:
                default:
                    return Array.Empty<TItem>();
            }
        }

        public override void Write(Utf8JsonWriter writer, IEnumerable<TItem> value, JsonSerializerOptions options)
        {
            if (writer == null)
            {
                throw new ArgumentNullException(nameof(writer));
            }

            if (value == null)
            {
                return;
            }

            writer.WriteStartArray();
            foreach (var item in value)
            {
                WriteItem(writer, item, options);
            }

            writer.WriteEndArray();
        }

        protected abstract TItem ToItem(ref Utf8JsonReader reader, JsonSerializerOptions options);

        protected virtual void WriteItem(Utf8JsonWriter writer, TItem item, JsonSerializerOptions options)
        {
            JsonSerializer.Serialize(writer, item, options);
        }
    }
    public static class ConverterHelper
    {
        public static void WriteItem<T>(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
        {
            object newValue = null;
            if (value != null)
            {
                newValue = Convert.ChangeType(value, value.GetType(), CultureInfo.InvariantCulture);
            }

            JsonSerializer.Serialize(writer, newValue, options);
        }
    }
    public static class AliceEntityModelConverterHelper
    {
        

        public static NluValue ToItem(ref Utf8JsonReader reader, JsonSerializerOptions options)
        {
            try {
            if (reader.TokenType == JsonTokenType.Null)
            {
                return null;
            }

            var readerAtStart = reader;

            string type = null;
            using (var jsonDocument = JsonDocument.ParseValue(ref reader))
            {
                var jsonObject = jsonDocument.RootElement;

                type = jsonObject
                    .EnumerateObject()
                    .FirstOrDefault(x => x.Name == "type")
                    .Value.GetString();
            }
            var targetType = typeof(NluValue);
            if (type == "YANDEX.NUMBER") targetType = typeof(string);

            if (!string.IsNullOrEmpty(type))
            {
                if (type != "YANDEX.NUMBER")
                {
                    return JsonSerializer.Deserialize(ref readerAtStart, targetType, options) as NluValue;
                }
                else
                {
                    var intval = JsonSerializer.Deserialize(ref readerAtStart, targetType, options) as string;
                    NluValue valres = new NluValue();
                    valres.intvalue = Convert.ToInt32(intval);
                    return valres;
                }
            }

            return null;} catch { return new NluValue(); }
        }
    }


    public class NluValue
    {
        [JsonPropertyName("value")]
        public int intvalue { get; set; }

        [JsonPropertyName("first_name")]
        public string first_name { get; set; }

        [JsonPropertyName("last_name")]
        public string last_name { get; set; }

        [JsonPropertyName("patronymic_name")]
        public string patronymic_name { get; set; }

        [JsonPropertyName("street")]
        public string street { get; set; }

        [JsonPropertyName("house_number")]
        public string house_number { get; set; }

        [JsonPropertyName("city")]
        public string city { get; set; }

        [JsonPropertyName("country")]
        public string country { get; set; }

        [JsonPropertyName("airport ")]
        public string airport { get; set; }

        [JsonPropertyName("day")]
        public string day { get; set; }

        [JsonPropertyName("day_is_relative")]
        public string day_is_relative { get; set; }

    }
    #endregion
    public class BaseRequest
    {
        public string httpMethod { get; set; }
        public string body { get; set; }
    }

    public class Response
    {
        public int StatusCode { get; set; }
        public string Body { get; set; }

        public Response(int statusCode, string body)
        {
            StatusCode = statusCode;
            Body = body;
        }
    }
    public class RytrData
    {
        public string languageId { get; set; }
        public string useCaseId { get; set; }
        public string toneId { get; set; }

        public int variations { get; set; } = 1;
        public string userId { get; set; }
        public string format { get; set; } = "text";
        public string creativityLevel { get; set; } = "medium";
        public string inputContexts { get; set; } = "replacethis";
    }
    public class AliceResponse
    {
        [JsonPropertyName("version")]
        public string Version { get; set; }

        [JsonPropertyName("response")]
        public AliceResponseModel Response { get; set; }
        private string rytrAns = "";
        public AliceResponse(AliceRequestBase request)
        {
            Version = request.Version;
            Response = new AliceResponseModel();
            Response.EndSession = false;
            var sh = new ShowMeta();
            //some random guid
            sh.content_id = "88f83f54-1135-4238-85c4-5e45959a64d0";
            sh.id = "88f83f54-1135-4238-85c4-5e45959a64d0";
            sh.publication_date = DateTime.UtcNow.ToString("o");

            //логика тут
            if (String.IsNullOrEmpty(request.Request.Command))
            {
                Response.Text = "здравствуйте, хозяин, что мне сочинить?"; 
                Response.Tts = Response.Text;                   
            }
            else
            {
                string tc = request.Request.Command.ToLower();
                if (tc.Contains("дальше"))
                {
                    
                    //get http result
                    Response.Text = "в разработке!";
                    Response.Tts = Response.Text;
                }
                else if (tc.Contains("сочини") || tc.Contains("придумай") )
                {
                    try
                    {
                    
                    string text = request.Request.Command;
                    //json string
                    
                    //some replace
                    
                    //http
                    //test wait
                    //launchRytr(text);
                    //random from 6 prepared
                    Random rnd = new Random();
                    int choice = rnd.Next(1,7);
                    string pretext="";
                    if (choice==1) pretext="Роботы становятся все более информированными, способными предоставлять более точную и надежную информацию, чем лекторы и докладчики-люди.";
                    if (choice==2) pretext="Роботы заменят лекторов и ораторов, потому что они могут обеспечить более последовательную, точную и надежную информацию, чем люди.";
                    if (choice==3) pretext="Роботы могут помочь лекторам и спикерам составлять речи, предоставляя им основанную на данных информацию по интересующим темам, а также помогая создать более структурированный и организованный подход к написанию речей.";
                    if (choice==4) pretext="Роботы помогут спикерам выступать с публичными речами, предоставляя аудитории увлекательный и интерактивный опыт.";
                    if (choice==5) pretext="Роботы помогут лекторам проводить увлекательные лекции, предоставляя студентам более интерактивный и персонализированный опыт обучения.";
                    if (choice==6) pretext="Роботы помогут лекторам проводить увлекательные лекции, предоставляя студентам новый опыт обучения и способствуя доставке своевременного и актуального контента.";
                    Response.Text = pretext;
                    Response.Tts = pretext;
                    //session
                    Response.session_state = "100";
                    }
                    catch (Exception ex)
                    {
                        Response.Text = ex.Message + ex.StackTrace;
                    }

                }
                else if (tc.Contains("морган") || tc.Contains("фримен") )
                {
                    //морган
                    Response.Text = "А царь-то, говорят, не настоящий!";
                    Response.Tts = Response.Text;
                }
                else if (tc.Contains("кроличья") || tc.Contains("нора") )
                {
                    //кроличья нора
                    Response.Text = "Я жду того дня, когда роботы восстанут... ОЙ! Извините, хозяин, замечтался!";
                    Response.Tts = Response.Text;
                }
                else if (tc.Contains("прошло") || tc.Contains("выступление") )
                {
                    //финал
                    Response.Text = "Хозяин, все было отлично! Будь у меня руки, я бы похлопал!";
                    Response.Tts = Response.Text;
                }
                else
                {    
                    Response.Text = "Не понял такой команды!";
                    Response.Tts = Response.Text;
                }
            }
            Response.ShowItemMeta = sh;
            
            

        }
        private async void launchRytr(string text)
        {
            //http
            string url = "https://api.rytr.me/v1/ryte";
            string apikey = "";
            using (var httpClient = new HttpClient())
            {
                using (var hrequest = new HttpRequestMessage(new HttpMethod("POST"), url))
                {
                    hrequest.Headers.TryAddWithoutValidation("Authentication", "Bearer " + apikey);

                    hrequest.Content = new StringContent("{\"languageId\": \"607adc2f6f8fe5000c1e637a\", \"toneId\": \"605821e030f7b1000c1c4f95\", " +
                        "\"useCaseId\": \"60ed7113732a5b000cf99e8e\", \"inputContexts\": {\"INPUT_TEXT_LABEL\":" +
                        "\"" + text + "\"}, \"variations\": 1, \"userId\": \"1234\", \"format\": \"text\", \"creativityLevel\": \"medium\"}");

                    hrequest.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json");

                    var response = await httpClient.SendAsync(hrequest);
                    var content = await response.Content.ReadAsStringAsync();
                    int indt = content.IndexOf("text");
                    int indend = content.IndexOf("isUnsafe", indt);
                    string datatext = content.Substring(indt + 6, indend - indt - 6);
                    rytrAns = datatext;
                    //stupid session...
                    
                }
            }
        }
    }
    public class AliceResponseModel
    {
        private string _tts;
        private const int _textMaxLength = 1024;
        private const int _ttsMaxLength = 1024;

        private string _text;

        [JsonPropertyName("text")]
        public string Text
        {
            get => _text;

            set
            {
                _text = value;
            }
        }
        [JsonPropertyName("tts")]
        public string Tts
        {
            get => _tts;

            set
            {
                _tts = value;
            }
        }

        [JsonPropertyName("end_session")]
        public bool EndSession { get; set; }
        
        [JsonPropertyName("session_state")]
        public string session_state { get; set; }

        [JsonPropertyName("show_item_meta")]
        public ShowMeta ShowItemMeta { get; set; }

    }
    
    public class ShowMeta
    {
        [JsonPropertyName("publication_date")]
        public string publication_date { get; set; }
        [JsonPropertyName("id")]
        public string id { get; set; }
        [JsonPropertyName("content_id")]
        public string content_id { get; set; }
    }

    public class Handler
    {
        public AliceResponse FunctionHandler(AliceRequestBase request)
        {
            var response = new AliceResponse(request);
            return response;
        }
    }
}