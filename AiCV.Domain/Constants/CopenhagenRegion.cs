namespace AiCV.Domain.Constants;

public static class CopenhagenRegion
{
    public const string RegionName = "storkoebenhavn";

    public static readonly string[] Places =
    [
        // Copenhagen proper and immediate surroundings
        "københavn", "kobenhavn", "kbh", "copenhagen", "cph",
        "frederiksberg", "amager", "kastrup", "ørestad", "orestad", "valby",
        "nordhavn", "sydhavn", "vanløse", "vanlose", "brønshøj", "bronshoj",

        // Northern suburbs
        "gentofte", "hellerup", "charlottenlund", "lyngby", "kongens lyngby",
        "gladsaxe", "søborg", "soborg", "bagsværd", "bagsvaerd", "herlev",
        "virum", "holte", "birkerød", "birkerod", "rudersdal", "søllerød", "sollerod",
        "vedbæk", "vedbaek", "skodsborg", "klampenborg", "dyssegård", "dyssegard",

        // Western suburbs
        "ballerup", "skovlunde", "glostrup", "albertslund", "rødovre", "rodovre",
        "brøndby", "brondby", "hvidovre", "vallensbæk", "vallensbaek",
        "taastrup", "høje-taastrup", "hoje-taastrup", "tåstrup", "tastrup",
        "ishøj", "ishoj", "greve", "hedehusene", "måløv", "malov", "smørum", "smorum",

        // Outer ring (~35-50 km)
        "roskilde", "hillerød", "hillerod", "farum", "værløse", "vaerlose",
        "allerød", "allerod", "frederikssund", "ølstykke", "olstykke",
        "stenløse", "stenlose", "solrød", "solrod", "køge", "koge",
        "helsingør", "helsingor", "fredensborg", "hørsholm", "horsholm",
        "espergærde", "espergaerde", "humlebæk", "humlebaek", "nivå", "niva",

        // Regional umbrella terms
        "storkøbenhavn", "storkobenhavn", "greater copenhagen", "copenhagen area",
        "hovedstaden", "hovedstadsområdet", "hovedstadsomradet",
        "region hovedstaden", "nordsjælland", "nordsjaelland",
        "københavns omegn", "kobenhavns omegn",
    ];

    public static readonly string[] ExcludedPlaces =
    [
        "aarhus", "århus", "aalborg", "ålborg", "odense", "esbjerg", "randers",
        "kolding", "horsens", "vejle", "silkeborg", "herning", "fredericia",
        "viborg", "holstebro", "skive", "sønderborg", "sonderborg", "svendborg",
        "nykøbing", "nykobing", "bornholm", "rønne", "ronne",
        "jylland", "jutland", "fyn", "funen", "midtjylland", "nordjylland",
        "syddanmark", "sydjylland", "østjylland", "ostjylland",
        "malmö", "malmo", "sverige", "sweden", "lund", "helsingborg",
    ];

    public static bool IsInScope(string? location)
    {
        if (string.IsNullOrWhiteSpace(location))
        {
            return false;
        }

        var normalized = location.ToLowerInvariant();

        if (ExcludedPlaces.Any(p => normalized.Contains(p, StringComparison.Ordinal)))
        {
            return false;
        }

        return Places.Any(p => normalized.Contains(p, StringComparison.Ordinal));
    }

    public static bool IsOutOfScope(string? location)
    {
        if (string.IsNullOrWhiteSpace(location))
        {
            return false;
        }

        var normalized = location.ToLowerInvariant();
        return ExcludedPlaces.Any(p => normalized.Contains(p, StringComparison.Ordinal));
    }
}
