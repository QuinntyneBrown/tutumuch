namespace TutuMuch.Cli;

/// <summary>One demo account. Every demo member signs in with <see cref="DemoCatalogue.Password"/>.</summary>
public sealed record DemoMember(string DisplayName, string Email);

/// <summary>
/// One demo listing as a seller would enter it. <see cref="Photos"/> are paths under the seed
/// photos folder, cover first. The size is never given: it comes from the category (L2-013).
/// </summary>
public sealed record DemoListing(
    string Category,
    string Subcategory,
    string Title,
    string Description,
    string Brand,
    string Condition,
    string? Flaws,
    decimal Price,
    decimal ShippingCost,
    IReadOnlyList<string> Photos);

/// <summary>
/// The fixed demo set (seed-demo-data design): 8 members and 48 listings, 12 per category. Brands
/// come from Selling:SuggestedBrands; photos are the committed original illustrations in
/// SeedPhotos/. A small demo, not a load or scale fixture (ADR general/0002).
/// </summary>
public static class DemoCatalogue
{
    /// <summary>The shared demo password, documented in the CLI README. For local use only.</summary>
    public const string Password = "TutuMuch-demo-only-2026";

    public static readonly IReadOnlyList<DemoMember> Members =
    [
        new("TutuTessa", "tessa@demo.tutumuch.test"),
        new("TwirlyTara", "tara@demo.tutumuch.test"),
        new("HandMeDownHana", "hana@demo.tutumuch.test"),
        new("PuddleJumperPia", "pia@demo.tutumuch.test"),
        new("RainbowRosa", "rosa@demo.tutumuch.test"),
        new("LeggingsLily", "lily@demo.tutumuch.test"),
        new("PartyDressPenny", "penny@demo.tutumuch.test"),
        new("CozyCoatCleo", "cleo@demo.tutumuch.test"),
    ];

    private const string PartyDress = "Dresses/pink-floral-party-dress.jpg";
    private const string Sundress = "Dresses/yellow-gingham-sundress.jpg";
    private const string TutuSkirt = "Dresses/pink-tulle-tutu-skirt.jpg";
    private const string PleatedSkirt = "Dresses/lavender-pleated-skirt.jpg";
    private const string Romper = "Dresses/mint-ruffle-romper.jpg";
    private const string TutuDress = "Dresses/lavender-tutu-dress.jpg";
    private const string FloralBlouse = "Tops/floral-peter-pan-blouse.jpg";
    private const string RuffleBlouse = "Tops/mint-ruffle-blouse.jpg";
    private const string RainbowSweater = "Tops/rainbow-knit-sweater.jpg";
    private const string Cardigan = "Tops/cream-heart-cardigan.jpg";
    private const string Hoodie = "Tops/lavender-heart-hoodie.jpg";
    private const string UnicornTee = "Tops/unicorn-graphic-t-shirt.jpg";
    private const string Raincoat = "Outerwear/yellow-raincoat.jpg";
    private const string DenimJacket = "Outerwear/light-wash-denim-jacket.jpg";
    private const string FleeceJacket = "Outerwear/mint-fleece-jacket.jpg";
    private const string WoolCoat = "Outerwear/pink-wool-coat.jpg";
    private const string DuffleCoat = "Outerwear/red-duffle-coat.jpg";
    private const string PufferJacket = "Outerwear/purple-puffer-jacket.jpg";
    private const string CordPants = "Bottoms/pink-corduroy-pants.jpg";
    private const string DenimLeggings = "Bottoms/denim-leggings.jpg";
    private const string Jeans = "Bottoms/light-wash-jeans.jpg";
    private const string FloralShorts = "Bottoms/floral-shorts.jpg";
    private const string DenimShorts = "Bottoms/denim-shorts.jpg";
    private const string BikeShorts = "Bottoms/yellow-bike-shorts.jpg";

    private const string NewWithTags = "New with tags";
    private const string NewWithoutTags = "New without tags";
    private const string Excellent = "Used - Excellent";
    private const string Good = "Used - Good";
    private const string Fair = "Used - Fair";

    private static readonly IReadOnlyList<DemoListing> Dresses =
    [
        new("Dresses", "Dresses", "Floral party dress, pink", "Pink cotton party dress with a white floral print, puff sleeves and a sash that ties at the back. Worn to one birthday party.", "Janie and Jack", Excellent, null, 38m, 6m, [PartyDress, Sundress]),
        new("Dresses", "Dresses", "Pink rosebud twirl dress", "Full-skirted dress made for spinning, with a soft lining and a hidden back zip. Worn twice for photos.", "Mini Boden", Excellent, null, 30m, 5m, [PartyDress]),
        new("Dresses", "Dresses", "Yellow gingham sundress", "Sunny yellow gingham sundress with wide straps and a smocked bodice. Tried on at home, never worn out.", "Hanna Andersson", NewWithoutTags, null, 34m, 6m, [Sundress, PartyDress]),
        new("Dresses", "Dresses", "Smocked cotton sundress, lemon", "Light cotton sundress in a lemon check, perfect for summer days. Soft from washing, no marks.", "Old Navy", Good, null, 12m, 5m, [Sundress]),
        new("Dresses", "Tutus", "Pink tulle tutu skirt", "Three layers of soft pink tulle on a comfy elastic waist. Lots of twirl left after a season of dance class.", "Carter's", Good, null, 14m, 5m, [TutuSkirt, PleatedSkirt]),
        new("Dresses", "Tutus", "Layered ballet tutu, blush pink", "Blush pink ballet tutu with a satin waistband and a little bow. Still has its tags.", "Tea Collection", NewWithTags, null, 28m, 5m, [TutuSkirt]),
        new("Dresses", "Skirts", "Lavender pleated skirt", "Pleated lavender skirt with an adjustable waist and built-in shorts underneath.", "Gap Kids", Fair, "A small pull in the pleats near the hem and some fading at the waistband.", 9m, 4m, [PleatedSkirt, TutuSkirt, TutuDress]),
        new("Dresses", "Skirts", "Lilac swing skirt with shorts", "Easy pull-on swing skirt in lilac jersey, with shorts underneath for climbing frames.", "H&M Kids", Excellent, null, 11m, 4m, [PleatedSkirt]),
        new("Dresses", "Rompers", "Mint ruffle romper", "Mint cotton romper with ruffled shoulders and snaps for quick changes. Worn once to check the fit.", "OshKosh B'gosh", NewWithoutTags, null, 22m, 5m, [Romper, TutuDress]),
        new("Dresses", "Rompers", "Seafoam flutter-sleeve romper", "Breezy seafoam romper with flutter sleeves and a tie waist. A little softer from washing.", "Carter's", Good, null, 10m, 4m, [Romper]),
        new("Dresses", "Tutus", "Lavender tutu dress with sparkle bodice", "Lavender tutu dress with a sparkly knit bodice and a full tulle skirt. Clean and ready to dance.", "Zara Kids", Excellent, null, 26m, 6m, [TutuDress, Romper, PleatedSkirt]),
        new("Dresses", "Tutus", "Fairy tutu dress, lilac", "Lilac fairy dress with layers of tulle and a satin bow. New in its bag, free shipping.", "Janie and Jack", NewWithTags, null, 45m, 0m, [TutuDress]),
    ];

    private static readonly IReadOnlyList<DemoListing> Tops =
    [
        new("Tops", "Shirts", "Floral Peter Pan collar blouse", "Cotton blouse with a sweet floral print and a rounded Peter Pan collar. Washed cold and hung dry.", "Hanna Andersson", Excellent, null, 16m, 4m, [FloralBlouse, RuffleBlouse]),
        new("Tops", "Shirts", "Ditsy floral button-up blouse", "Soft button-up blouse in a tiny pink floral print. Tags still attached.", "Mini Boden", NewWithTags, null, 24m, 4m, [FloralBlouse]),
        new("Tops", "Shirts", "Mint ruffle blouse", "Mint cotton blouse with ruffles along the yoke and pearl-style buttons. Washed once.", "Gap Kids", NewWithoutTags, null, 15m, 4m, [RuffleBlouse, FloralBlouse]),
        new("Tops", "Shirts", "Ruffle-yoke cotton top, mint", "Light mint top with a ruffled yoke for warm days. Slight fading at the collar.", "Old Navy", Good, null, 7m, 4m, [RuffleBlouse]),
        new("Tops", "Sweaters", "Rainbow knit sweater", "Chunky knit sweater striped in every colour of the rainbow. Soft, cosy and like new.", "Tea Collection", Excellent, null, 26m, 6m, [RainbowSweater, Cardigan]),
        new("Tops", "Sweaters", "Rainbow stripe crewneck", "Cotton crewneck in rainbow stripes, roomy enough to layer over a tee.", "H&M Kids", Fair, "Light pilling on the sleeves and one small mended spot at the cuff.", 6m, 4m, [RainbowSweater]),
        new("Tops", "Sweaters", "Cream heart cardigan", "Cream button-up cardigan with little knitted pink hearts. Never worn.", "Janie and Jack", NewWithoutTags, null, 32m, 5m, [Cardigan, RainbowSweater]),
        new("Tops", "Sweaters", "Heart-button knit cardigan, ivory", "Everyday ivory cardigan with heart-shaped buttons. Some softening from washing, no marks.", "Carter's", Good, null, 9m, 4m, [Cardigan]),
        new("Tops", "Hoodies", "Lavender heart hoodie", "Fleece-lined lavender hoodie with a kangaroo pocket and a big printed heart.", "Old Navy", Good, null, 12m, 5m, [Hoodie, UnicornTee]),
        new("Tops", "Hoodies", "Lilac zip hoodie with heart print", "Cosy lilac zip hoodie that keeps its shape. Bought the wrong size, tags on.", "Gap Kids", NewWithTags, null, 28m, 5m, [Hoodie]),
        new("Tops", "T-shirts", "Unicorn graphic T-shirt", "Soft white tee with a rainbow-maned unicorn print. A favourite, still bright.", "Carter's", Excellent, null, 8m, 3m, [UnicornTee, Hoodie]),
        new("Tops", "T-shirts", "Rainbow unicorn tee", "Cotton tee with a glittery unicorn print. New, still folded from the shop.", "Zara Kids", NewWithTags, null, 14m, 3m, [UnicornTee]),
    ];

    private static readonly IReadOnlyList<DemoListing> Outerwear =
    [
        new("Outerwear", "Jackets", "Yellow rain jacket with hood", "Bright yellow waterproof rain jacket with a peaked hood and reflective trim. Kept her dry all spring.", "Hanna Andersson", Excellent, null, 36m, 7m, [Raincoat, FleeceJacket]),
        new("Outerwear", "Jackets", "Packable puddle jacket, yellow", "Lightweight waterproof jacket that folds into its own pocket. Light wear on the cuffs.", "Old Navy", Good, null, 14m, 6m, [Raincoat]),
        new("Outerwear", "Jackets", "Light-wash denim jacket", "Classic light-wash denim jacket with button pockets. Softened with wear, all buttons present.", "Gap Kids", Good, null, 18m, 6m, [DenimJacket, Raincoat]),
        new("Outerwear", "Jackets", "Embroidered daisy denim jacket", "Denim jacket with tiny embroidered daisies on the collar. Unworn.", "Zara Kids", NewWithoutTags, null, 32m, 6m, [DenimJacket]),
        new("Outerwear", "Jackets", "Mint fleece zip jacket", "Warm mint fleece jacket with zipped pockets. Great as a layer under a raincoat.", "OshKosh B'gosh", Excellent, null, 16m, 5m, [FleeceJacket, DenimJacket]),
        new("Outerwear", "Jackets", "Sherpa fleece jacket, seafoam", "Fuzzy seafoam sherpa jacket with a stand collar and elastic cuffs.", "Carter's", Fair, "Pilling on the cuffs and a faint mark on the back.", 8m, 5m, [FleeceJacket]),
        new("Outerwear", "Coats", "Pink wool coat with bow buttons", "Rose pink wool-blend coat with a Peter Pan collar and bow buttons, lined for winter.", "Janie and Jack", Excellent, null, 58m, 8m, [WoolCoat, DuffleCoat]),
        new("Outerwear", "Coats", "Rose wool-blend dress coat", "Smart rose dress coat in a soft wool blend, perfect for holidays.", "Mini Boden", Good, null, 34m, 8m, [WoolCoat]),
        new("Outerwear", "Coats", "Red duffle coat with toggles", "Classic red duffle coat with wooden toggles and a cosy hood. Tags attached.", "Mini Boden", NewWithTags, null, 60m, 8m, [DuffleCoat, WoolCoat]),
        new("Outerwear", "Coats", "Toggle duffle coat, cherry red", "Warm cherry red duffle with a check lining and patch pockets. Worn one season.", "Hanna Andersson", Good, null, 30m, 8m, [DuffleCoat]),
        new("Outerwear", "Jackets", "Purple puffer jacket", "Warm purple puffer with a fleece-lined hood and zipped pockets. Ready for snow days.", "Gap Kids", Excellent, null, 42m, 8m, [PufferJacket, Raincoat, DenimJacket, FleeceJacket]),
        new("Outerwear", "Jackets", "Quilted puffer, grape", "Well-loved grape puffer jacket with a two-way zip and snug cuffs.", "Old Navy", Fair, "Creasing at the elbows and a worn zip pull.", 15m, 7m, [PufferJacket]),
    ];

    private static readonly IReadOnlyList<DemoListing> Bottoms =
    [
        new("Bottoms", "Pants", "Pink corduroy pants", "Soft pink corduroy pants with an elastic waist and roll-up cuffs. Washed many times, still sturdy.", "OshKosh B'gosh", Good, null, 10m, 5m, [CordPants, DenimLeggings]),
        new("Bottoms", "Pants", "Pull-on cord pants, bubblegum", "Bubblegum pink pull-on cords with an adjustable waist. New with tags.", "Carter's", NewWithTags, null, 18m, 5m, [CordPants]),
        new("Bottoms", "Leggings", "Denim leggings", "Stretchy denim-look leggings with faux front pockets. Never worn.", "Gap Kids", NewWithoutTags, null, 16m, 4m, [DenimLeggings, CordPants]),
        new("Bottoms", "Leggings", "Stretch jeggings, mid blue", "Everyday pull-on jeggings in a mid blue wash. No stains or wear marks.", "Old Navy", Excellent, null, 8m, 4m, [DenimLeggings]),
        new("Bottoms", "Pants", "Light-wash straight jeans", "Light-wash denim jeans with a straight leg and an adjustable waist.", "OshKosh B'gosh", Good, null, 12m, 5m, [Jeans, CordPants]),
        new("Bottoms", "Pants", "Worn-in light-wash jeans", "Properly playground-tested jeans with honest wear and lots of life left.", "H&M Kids", Fair, "Fraying at the hems and fading on the knees.", 5m, 5m, [Jeans]),
        new("Bottoms", "Shorts", "Floral cotton shorts", "Breathable cotton shorts in a bright floral print with a drawstring waist.", "Tea Collection", NewWithTags, null, 18m, 4m, [FloralShorts, DenimShorts]),
        new("Bottoms", "Shorts", "Flower-print pull-on shorts", "Pull-on shorts in a pink flower print for hot days, with a ruffled hem.", "Hanna Andersson", Excellent, null, 12m, 4m, [FloralShorts]),
        new("Bottoms", "Shorts", "Denim shorts with rolled hems", "Soft denim shorts with rolled hems and a comfy elastic back waist.", "Gap Kids", Good, null, 9m, 4m, [DenimShorts, BikeShorts]),
        new("Bottoms", "Shorts", "Heart-pocket denim shorts", "Denim shorts with little heart-shaped back pockets. Never worn.", "Zara Kids", NewWithoutTags, null, 16m, 4m, [DenimShorts]),
        new("Bottoms", "Shorts", "Yellow bike shorts", "Sunny yellow stretch bike shorts, perfect under skirts and dresses.", "H&M Kids", Excellent, null, 7m, 3m, [BikeShorts, FloralShorts, DenimShorts]),
        new("Bottoms", "Shorts", "Sunshine cotton bike shorts", "Bright summer bike shorts in soft stretch cotton. New with tags, free shipping.", "Old Navy", NewWithTags, null, 10m, 0m, [BikeShorts]),
    ];

    /// <summary>
    /// Every listing in publish order, oldest first. The categories alternate, so the newest
    /// listings on the home page mix dresses, tops, outerwear and bottoms, and the two listings
    /// that share a cover photo are published six apart instead of side by side.
    /// </summary>
    public static readonly IReadOnlyList<DemoListing> Listings = Interleave(Spread(Dresses), Spread(Tops), Spread(Outerwear), Spread(Bottoms));

    /// <summary>Every photo file the catalogue uses, relative to the seed photos folder.</summary>
    public static IReadOnlyList<string> PhotoFiles { get; } = Listings.SelectMany(l => l.Photos).Distinct().Order(StringComparer.Ordinal).ToList();

    /// <summary>The even entries, then the odd ones.</summary>
    private static List<DemoListing> Spread(IReadOnlyList<DemoListing> listings) =>
        listings.Where((_, i) => i % 2 == 0).Concat(listings.Where((_, i) => i % 2 == 1)).ToList();

    private static List<DemoListing> Interleave(params List<DemoListing>[] categories) =>
        Enumerable.Range(0, categories[0].Count).SelectMany(i => categories.Select(c => c[i])).ToList();

    /// <summary>The member who sells listing <paramref name="index"/>: sellers take turns.</summary>
    public static DemoMember SellerOf(int index) => Members[index % Members.Count];
}
