using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Azure;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Luis;
using Microsoft.Bot.Builder.Luis.Models;
using Microsoft.Bot.Connector;
using Newtonsoft.Json;
using Microsoft.Bot.Builder.CognitiveServices.QnAMaker;
using System.Threading;

// For more information about this template visit http://aka.ms/azurebots-csharp-luis
[Serializable]
public class BasicLuisDialog : LuisDialog<object>


{
    public BasicLuisDialog() : base(new LuisService(new LuisModelAttribute(Utils.GetAppSetting("LuisAppId"), Utils.GetAppSetting("LuisAPIKey"))))
    {
        FemaleIdentifiers = new List<string>
        {
            "mother",
            "mom",
            "grandmother",
            "girlfriend",
            "sister",
            "daughter",
            "girl"
        };
        MaleIdentifiers = new List<string>
        {
            "father",
            "dad",
            "brother",
            "son",
            "boy",
            "boyfriend"
        };

        Products = new List<Product>()
        {
            new Product() {Name = "Havehandsker", Gender = GenderFemale, Id = "HAVE1", Interest = "gardening"},
            new Product() {Name = "Skovl", Gender = GenderMale, Id = "SKOV5", Interest = "gardening"},
            new Product() {Name = "Yogamat", Gender = GenderFemale, Id = "YOGA1", Interest = "fitness"},
            new Product() {Name = "Garmin watch", Gender = GenderMale, Id = "GARM9", Interest = "fitness"}
        };
    }

    public const string PersonEntityKey = "Person";
    public const string ProductEntityKey = "Product";
    public const string AgeEntityKey = "builtin.age";
    public const string GenderEntityKey = "Gender";
    public const string InterestEntityKey = "Interest";

    public const string GenderMale = "Male";
    public const string GenderFemale = "Female";

    public List<string> FemaleIdentifiers;
    public List<string> MaleIdentifiers;
    public List<Product> Products;

    [Serializable]
    public class Product
    {
        public string Name { get; set; }
        public string Id { get; set; }
        public string Interest { get; set; }
        public string Gender { get; set; }
    }

    [LuisIntent("None")]
    public async Task NoneIntent(IDialogContext context, LuisResult result)
    {
        await context.PostAsync($"You have reached the none intent. You said: {result.Query}"); //
        context.Wait(MessageReceived);
    }

    [LuisIntent("Greeting")]
    public async Task Greeting(IDialogContext context, LuisResult result)
    {
        await context.PostAsync($"Hello. How can I be of service."); //
        context.Wait(MessageReceived);
    }

    // Go to https://luis.ai and create a new intent, then train/publish your luis app.
    // Finally replace "MyIntent" with the name of your newly created intent in the following handler
    [LuisIntent("GetInspiration")]
    public async Task GetInspiration(IDialogContext context, LuisResult result)
    {
        context.ConversationData.Clear();
        //Check hvilke entiteter der er identificeret.
        FetchInspirationData(context, result);
        await ProceedInspirationConversation(context, result);
    }

    [LuisIntent("IdentifyPerson")]
    public async Task IdentifyPerson(IDialogContext context, LuisResult result)
    {
        FetchInspirationData(context, result);
        string person;
        if (TryGetConversationData(context, PersonEntityKey, out person))
        {
            await ProceedInspirationConversation(context, result);
        }
        else
        {
            await context.PostAsync($"I am not clever enough to understand you?"); //
            context.Wait(MessageReceived);
        }

    }

    [LuisIntent("IdentifyGender")]
    public async Task IdentifySex(IDialogContext context, LuisResult result)
    {
        FetchInspirationData(context, result);
        await ProceedInspirationConversation(context, result);
    }

    [LuisIntent("IdentifyAge")]
    public async Task IdentifyAge(IDialogContext context, LuisResult result)
    {
        FetchInspirationData(context, result);
        await ProceedInspirationConversation(context, result);
    }

    [LuisIntent("IdentifyInterests")]
    public async Task IdentifyInterests(IDialogContext context, LuisResult result)
    {
        FetchInspirationData(context, result);
        await ProceedInspirationConversation(context, result);
    }



    private async Task ProceedInspirationConversation(IDialogContext context, LuisResult result)
    {
        string person;
        string gender;
        if (!TryGetConversationData(context, PersonEntityKey, out person))
        {
            await context.PostAsync($"Who do you want to buy a gift for?"); //
        }
        else if (!TryGetConversationData(context, GenderEntityKey, out gender))
        {
            await context.PostAsync($"Is {person} male or female?");
        }
        else if (!HasConversationData(context, AgeEntityKey))
        {
            await context.PostAsync($"How old is {person}?");
        }
        else if (!HasConversationData(context, InterestEntityKey))
        {
            await context.PostAsync($"What are {person}s interests?");
        }
        else
        {
            string interests;
            TryGetConversationData(context, InterestEntityKey, out interests);
            //var interestsList = interests.Split(',').Select(x => x.ToLowerInvariant()).ToList();
            //var product =
            //    Products.Where(
            //        x =>
            //            x.Gender.Equals(gender, StringComparison.InvariantCultureIgnoreCase) &&
            //            interestsList.Any(y => y.Equals(x.Interest, StringComparison.InvariantCultureIgnoreCase)));
            //var products = string.Join(", ", product.Select(x => x.Id));

            var httpClient = new HttpClient();
            var translatedJson =
                await httpClient.GetStringAsync($"http://www.transltr.org/api/translate?text={gender}%20{interests}%20{person}&to=da&from=en");
            dynamic translation = JsonConvert.DeserializeObject(translatedJson);
            var searchText = translation.translationText.Value.Replace(" ", "%20");

            var resultJson = await httpClient.GetStringAsync($"http://politiken.dk/plus/side/soeg/MoreResults/?searchText={searchText}&skip=0&sorting=0&take=3");
            dynamic jsonResponse = JsonConvert.DeserializeObject(resultJson);

            if (jsonResponse.SearchResults.Count > 0)
            {
                var firstResult = jsonResponse.SearchResults[0];

                //firstResult.PriceStructure.PlusPriceText
                var suggestionUrl = $"http://politiken.dk/plus/side/soeg/#?searchText={searchText}";

                var replyMessage = context.MakeMessage();
                replyMessage.Attachments = new List<Attachment> { new Attachment {
                    Name = "Product.jpg",
                    ContentType = "image/jpg",
                    ContentUrl = $"http://politiken.dk/plus{firstResult.MediaUrl}"
                }};

                replyMessage.Text =
                    $"What about a {firstResult.Headline} ({firstResult.ContentUrl})? You can see more suggestions here: {suggestionUrl}";

                await context.PostAsync(replyMessage);
            }
            else
            {
                await context.PostAsync($"Does {person} have any other interests than {interests}?");
            }
            //await context.PostAsync($"Now I know everything {interests} , do you think {person} would like {products} based on gender {gender} and interests {interests}");
        }
        context.Wait(MessageReceived);
    }

    private bool HasConversationData(IDialogContext context, string key)
    {
        string value;
        if (context.ConversationData.TryGetValue(key, out value))
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    private bool TryGetConversationData(IDialogContext context, string key, out string value)
    {
        value = null;
        if (context.ConversationData.TryGetValue(key, out value))
        {
            return true;
        }
        else
        {
            return false;
        }
    }
    private bool TryGetEntityData(LuisResult result, string key, out string value)
    {
        value = null;

        var entity = result.Entities.FirstOrDefault(x => x.Type.Equals(key, StringComparison.InvariantCultureIgnoreCase) && x.Score > 0.4);
        if (entity == null) return false;
        value = entity.Entity;
        return true;
    }

    private IList<string> GetEntityDataList(LuisResult result, string key)
    {
        var entity = result.Entities.Where(x => x.Type.Equals(key, StringComparison.InvariantCultureIgnoreCase) && x.Score > 0.4).Select(x => x.Entity).ToList();
        return entity;
    }

    private void FetchInspirationData(IDialogContext context, LuisResult result)
    {
        var entities = new string[] { PersonEntityKey, AgeEntityKey, GenderEntityKey };
        foreach (var entityKey in entities)
        {
            string data = "";
            if (TryGetEntityData(result, entityKey, out data))
            {
                context.ConversationData.SetValue(entityKey, data);
            }
        }
        var interests = GetEntityDataList(result, InterestEntityKey);
        if (interests.Any())
            context.ConversationData.SetValue(InterestEntityKey, string.Join(",", interests.ToArray()));
        string person = "";
        if (TryGetEntityData(result, PersonEntityKey, out person))
        {
            person = person.ToLower();
            if (FemaleIdentifiers.Any(x => x.Equals(person, StringComparison.InvariantCultureIgnoreCase)))
            {
                context.ConversationData.SetValue(GenderEntityKey, GenderFemale);
            }
            else if (MaleIdentifiers.Any(x => x.Equals(person, StringComparison.InvariantCultureIgnoreCase)))
            {
                context.ConversationData.SetValue(GenderEntityKey, GenderMale);
            }
        }
    }




    // Go to https://luis.ai and create a new intent, then train/publish your luis app.
    // Finally replace "MyIntent" with the name of your newly created intent in the following handler
    [LuisIntent("Return")]
    public async Task Return(IDialogContext context, IAwaitable<IMessageActivity> message, LuisResult result)
    {
        //await context.PostAsync($"You have reached the Return intent. You said: {result.Query}"); //
        //context.Wait(MessageReceived);
        var faqDialog = new FaqDialog();

        var messageToForward = await message;
        await context.Forward(faqDialog, AfterQnADialog, messageToForward, CancellationToken.None);
    }

    [LuisIntent("Clear")]
    public async Task Clear(IDialogContext context, LuisResult result)
    {
        await context.PostAsync($"You have reached the Clear intent"); //
        context.ConversationData.Clear();
        context.Wait(MessageReceived);
    }

    [LuisIntent("ShowProduct")]
    public async Task ShowProduct(IDialogContext context, LuisResult result)
    {
        string productId;
        if (TryGetEntityData(result, ProductEntityKey, out productId))
        {
            var product = Products.FirstOrDefault(x => x.Id.Equals(productId, StringComparison.InvariantCultureIgnoreCase));
            if (product != null)
            {
                await context.PostAsync($"What a fine product Code {product.Id}, Name {product.Name}"); //

            }
            else
            {
                await context.PostAsync($"Unknown product"); //
            }
        }


        context.Wait(MessageReceived);
    }

    [LuisIntent("FAQ")]
    public async Task Faq(IDialogContext context, IAwaitable<IMessageActivity> message, LuisResult result)
    {
        var faqDialog = new FaqDialog();

        var messageToForward = await message;
        await context.Forward(faqDialog, AfterQnADialog, messageToForward, CancellationToken.None);
    }

    private async Task AfterQnADialog(IDialogContext context, IAwaitable<object> result)
    {
        await context.PostAsync("After qna");
        var answerFound = (bool)await result;

        // we might want to send a message or take some action if no answer was found (false returned)
        if (!answerFound) {
            await context.PostAsync("I’m not sure what you want.");
        }
        context.Done<object>(null);
        //context.Wait(MessageReceived);
    }

    [Serializable]
    public class FaqDialog : QnAMakerDialog {
        //Parameters to QnAMakerService are:
        //Compulsory: subscriptionKey, knowledgebaseId, 
        //Optional: defaultMessage, scoreThreshold[Range 0.0 – 1.0]
        public FaqDialog() : base(new QnAMakerService(new QnAMakerAttribute(Utils.GetAppSetting("QnASubscriptionKey"), Utils.GetAppSetting("QnAKnowledgebaseId")))) { }
        
        protected override async Task RespondFromQnAMakerResultAsync(IDialogContext context, IMessageActivity message, QnAMakerResult result) {
            await base.RespondFromQnAMakerResultAsync(context, message, result);
        }

        protected override async Task DefaultWaitNextMessageAsync(IDialogContext context, IMessageActivity message, QnAMakerResult result) {
            if (result != null && result.Score < 0.1) {
                //PromptDialog.Confirm(context, PromptDialogResultAsync, "Do you want to see the services menu?");
                context.Done<object>(null);
            }
            else {
                await base.DefaultWaitNextMessageAsync(context, message, result);
            }
        }

        private async Task PromptDialogResultAsync(IDialogContext context, IAwaitable<bool> result) {
            if (await result == true) {
                await context.PostAsync("Showing the menu..");

                // TODO: you can continue your custom logic here and finally go back to the QnA dialog loop using DefaultWaitNextMessageAsync()

                await this.DefaultWaitNextMessageAsync(context, null, null);
            }
            else {
                await this.DefaultWaitNextMessageAsync(context, null, null);
            }
        }
    }
}