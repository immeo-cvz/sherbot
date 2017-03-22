using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Azure;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Luis;
using Microsoft.Bot.Builder.Luis.Models;
using Newtonsoft.Json;

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
            new Product() {Name = "Skovl", Gender = GenderFemale, Id = "SKOV5", Interest = "gardening"},
            new Product() {Name = "Yogamat", Gender = GenderFemale, Id = "YOGA1", Interest = "fitness"},
            new Product() {Name = "Garmin watch", Gender = GenderFemale, Id = "GARM9", Interest = "fitness"}
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
            var interestsList = interests.Split(',').Select(x => x.ToLowerInvariant()).ToList();
            var product =
                Products.Where(
                    x =>
                        x.Gender.Equals(gender, StringComparison.InvariantCultureIgnoreCase) &&
                        interestsList.Any(y => y.Equals(x.Interest, StringComparison.InvariantCultureIgnoreCase));
            var products = string.Join(", ", product.Select(x => x.Id));
            await context.PostAsync($"Now I know everything {interests} , do you think {person} would like {products}");
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
    public async Task Return(IDialogContext context, LuisResult result)
    {
        await context.PostAsync($"You have reached the Return intent. You said: {result.Query}"); //
        context.Wait(MessageReceived);
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





}