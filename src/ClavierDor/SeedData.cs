namespace ClavierDor;

public static class SeedData
{
    private static readonly List<Question> Questions = new()
    {
        new Question
        {
            Prompt = "Quel est le résultat de 2 ** 3 en Python ?",
            ChoiceA = "6",
            ChoiceB = "8",
            ChoiceC = "9",
            ChoiceD = "12",
            CorrectChoice = "B",
            Stage = 1,
            Hint = "L'opérateur ** signifie puissance."
        },
        new Question
        {
            Prompt = "Quelle commande Git permet de lister les branches locales ?",
            ChoiceA = "git status",
            ChoiceB = "git branch",
            ChoiceC = "git log",
            ChoiceD = "git checkout",
            CorrectChoice = "B",
            Stage = 1,
            Hint = "C'est la commande la plus courte de la liste."
        },
        new Question
        {
            Prompt = "Quel protocole est utilisé par défaut pour sécuriser HTTP ?",
            ChoiceA = "FTP",
            ChoiceB = "SSH",
            ChoiceC = "HTTPS",
            ChoiceD = "SMTP",
            CorrectChoice = "C",
            Stage = 2,
            Hint = "Ajoutez un 'S' à HTTP."
        },
        new Question
        {
            Prompt = "Quel langage est principalement utilisé pour le développement iOS natif ?",
            ChoiceA = "Kotlin",
            ChoiceB = "Swift",
            ChoiceC = "Ruby",
            ChoiceD = "Go",
            CorrectChoice = "B",
            Stage = 2,
            Hint = "Un langage créé par Apple en 2014."
        },
        new Question
        {
            Prompt = "Quel design pattern favorise une instance unique et globale ?",
            ChoiceA = "Singleton",
            ChoiceB = "Observer",
            ChoiceC = "Strategy",
            ChoiceD = "Decorator",
            CorrectChoice = "A",
            Stage = 3,
            Hint = "Il signifie littéralement 'unique'."
        },
        new Question
        {
            Prompt = "Quelle méthode HTTP est utilisée pour créer une ressource ?",
            ChoiceA = "GET",
            ChoiceB = "POST",
            ChoiceC = "DELETE",
            ChoiceD = "PATCH",
            CorrectChoice = "B",
            Stage = 3,
            Hint = "On l'utilise souvent pour envoyer un formulaire."
        }
    };

    public static void EnsureSeeded(GameDbContext context)
    {
        if (context.Questions.Any())
        {
            return;
        }

        context.Questions.AddRange(Questions);
        context.SaveChanges();
    }
}
