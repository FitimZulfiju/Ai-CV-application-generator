namespace AiCV.Infrastructure.Services.Matching;

public class KeywordJobMatcher() : IJobMatcher
{
    private const int SkillWeight = 3;
    private const int TitleKeywordWeight = 2;
    private const int DescriptionKeywordWeight = 1;
    private const int LocationBonus = 5;
    private const int OutOfScopeLocationPenalty = -25;
    private const double ScoreCeiling = 45.0;

    private static readonly HashSet<string> KnownTechKeywords =
    [
        ".net", "c#", "csharp", "azure", "sql", "sqlserver", "entity framework", "ef core", "asp.net",
        "asp.net core", "blazor", "react", "angular", "vue", "javascript", "typescript", "node", "node.js",
        "docker", "kubernetes", "k8s", "microservices", "rest", "restful", "web api", "graphql", "redis",
        "rabbitmq", "kafka", "postgresql", "mongodb", "cicd", "ci/cd", "devops", "git", "cloud", "aws",
        "gcp", "terraform", "python", "java", "golang", "go", "php", "ruby", "swift", "kotlin", "bootstrap",
        "html", "css", "maui", "xamarin", "wpf", "winforms", "signalr", "grpc", "oauth", "jwt", "identity",
        "linq", "mvc", "mvvm", "solid", "tdd", "unit test", "xunit", "nunit", "moq", "serilog", "automapper"
    ];

    private static readonly HashSet<string> Stopwords =
    [
        "and", "or", "the", "a", "an", "of", "in", "on", "at", "to", "for", "with", "by", "from",
        "as", "is", "are", "was", "were", "be", "been", "being", "have", "has", "had", "do", "does",
        "did", "will", "would", "can", "could", "should", "may", "might", "must", "shall",
        "this", "that", "these", "those", "it", "its", "we", "our", "us", "you", "your", "they",
        "their", "he", "she", "his", "her", "i", "my", "me",
        "built", "build", "using", "used", "use", "work", "worked", "working", "working with",
        "experience", "years", "year", "strong", "good", "great", "various", "several", "including",
        "well", "also", "more", "most", "other", "such", "than", "then", "there", "here", "when",
        "where", "which", "who", "how", "all", "any", "both", "each", "some", "very", "over",
        "og", "eller", "en", "et", "den", "det", "de", "at", "af", "til", "for", "med", "på",
        "som", "er", "var", "har", "havde", "vi", "du", "din", "jeg", "min", "sig", "ikke",
        "erfaring", "år", "arbejde", "arbejder", "stærk", "god", "flere", "samt", "eller",
    ];

    public Task<List<MatchedJob>> MatchAsync(
        CandidateProfile profile,
        List<JobSearchResult> jobs,
        int topN = 10,
        CancellationToken cancellationToken = default)
    {
        var skills = ExtractSkills(profile);
        var keywords = ExtractKeywords(profile, skills);

        var matched = jobs
            .Select(job => ScoreJob(job, skills, keywords))
            .Where(s => s.Score > 0)
            .OrderByDescending(s => s.Score)
            .ThenByDescending(s => s.Job.DatePosted)
            .Take(Math.Max(0, topN))
            .ToList();

        return Task.FromResult(matched);
    }

    private static List<string> ExtractSkills(CandidateProfile profile)
    {
        var skills = new List<string>();

        foreach (var skill in profile.Skills)
        {
            AddTerm(skills, skill.Name);
        }

        foreach (var text in profile.WorkExperience
            .Select(e => e.Description)
            .Concat(profile.Projects.Select(p => p.Description))
            .Concat(profile.Projects.Select(p => p.Technologies)))
        {
            foreach (var token in Tokenize(text).Where(KnownTechKeywords.Contains))
            {
                AddTerm(skills, token);
            }
        }

        return Distinct(skills);
    }

    private static List<string> ExtractKeywords(CandidateProfile profile, List<string> skills)
    {
        var keywords = new List<string>();

        foreach (var exp in profile.WorkExperience)
        {
            AddTerm(keywords, exp.JobTitle);
            foreach (var token in Tokenize(exp.JobTitle))
            {
                AddTerm(keywords, token);
            }
        }

        AddTerm(keywords, profile.Title);
        foreach (var token in Tokenize(profile.Title))
        {
            AddTerm(keywords, token);
        }

        foreach (var token in Tokenize(profile.ProfessionalSummary))
        {
            AddTerm(keywords, token);
        }

        var skillSet = new HashSet<string>(skills, StringComparer.OrdinalIgnoreCase);
        return [.. Distinct(keywords).Where(k => !skillSet.Contains(k))];
    }

    private static void AddTerm(List<string> list, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        var normalized = value.Trim().ToLowerInvariant();

        if (normalized.Length is < 2 or > 40)
        {
            return;
        }

        if (Stopwords.Contains(normalized))
        {
            return;
        }

        if (normalized.All(c => char.IsDigit(c) || c is '.' or '-' or '/'))
        {
            return;
        }

        if (KnownTechKeywords.Contains(normalized) || normalized.Split(' ').Length <= 3)
        {
            list.Add(normalized);
        }
    }

    private static IEnumerable<string> Tokenize(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            yield break;
        }

        foreach (var word in text.Split(
                     [' ', ',', ';', ':', '.', '!', '?', '(', ')', '[', ']', '{', '}', '"', '\'', '\t', '\r', '\n', '|', '/', '\\'],
                     StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var trimmed = word.Trim('-', '–', '—', '*', '•');
            if (trimmed.Length > 1)
            {
                yield return trimmed.ToLowerInvariant();
            }
        }
    }

    private static List<string> Distinct(List<string> terms) => [.. terms
        .Where(t => !string.IsNullOrWhiteSpace(t))
        .Distinct(StringComparer.OrdinalIgnoreCase)];

    public static bool ContainsWord(string haystack, string term)
    {
        if (string.IsNullOrEmpty(haystack) || string.IsNullOrEmpty(term))
        {
            return false;
        }

        var index = haystack.IndexOf(term, StringComparison.Ordinal);
        while (index >= 0)
        {
            var startsCleanly = index == 0 || !IsWordChar(haystack[index - 1]);

            var endIndex = index + term.Length;
            var endsCleanly = endIndex >= haystack.Length || !IsWordChar(haystack[endIndex]);

            if (startsCleanly && endsCleanly)
            {
                return true;
            }

            index = haystack.IndexOf(term, index + 1, StringComparison.Ordinal);
        }

        return false;

        static bool IsWordChar(char c) => char.IsLetterOrDigit(c) || c is '#' or '+' or '.';
    }

    private static MatchedJob ScoreJob(JobSearchResult job, List<string> skills, List<string> keywords)
    {
        var title = job.Title.ToLowerInvariant();
        var description = job.DescriptionSnippet.ToLowerInvariant();

        var matchedSkills = new List<string>();
        var matchedKeywords = new List<string>();
        var score = 0;

        foreach (var skill in skills)
        {
            if (ContainsWord(title, skill) || ContainsWord(description, skill))
            {
                matchedSkills.Add(skill);
                score += SkillWeight;
            }
        }

        foreach (var keyword in keywords)
        {
            if (ContainsWord(title, keyword))
            {
                matchedKeywords.Add(keyword);
                score += TitleKeywordWeight;
            }
            else if (ContainsWord(description, keyword))
            {
                matchedKeywords.Add(keyword);
                score += DescriptionKeywordWeight;
            }
        }

        if (score <= 0)
        {
            return new MatchedJob(job, 0, matchedSkills, matchedKeywords);
        }

        if (CopenhagenRegion.IsOutOfScope(job.Location))
        {
            score += OutOfScopeLocationPenalty;
        }
        else if (CopenhagenRegion.IsInScope(job.Location))
        {
            score += LocationBonus;
        }

        var normalized = Math.Clamp((int)Math.Round(score / ScoreCeiling * 100.0), 0, 100);
        return new MatchedJob(job, normalized, matchedSkills, matchedKeywords);
    }
}
