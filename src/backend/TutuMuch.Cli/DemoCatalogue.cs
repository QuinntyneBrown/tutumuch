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
/// come from Selling:SuggestedBrands; photos are the committed CC BY 2.0 files in SeedPhotos/.
/// A small demo, not a load or scale fixture (ADR general/0002).
/// </summary>
public static class DemoCatalogue
{
    /// <summary>The shared demo password, documented in the CLI README. For local use only.</summary>
    public const string Password = "Tutu Much-demo-only-2026";

    public static readonly IReadOnlyList<DemoMember> Members =
    [
        new("BigFootBen", "ben@demo.tutumuch.test"),
        new("TallTomas", "tomas@demo.tutumuch.test"),
        new("SizeTwelveSam", "sam@demo.tutumuch.test"),
        new("LargeLayers", "layers@demo.tutumuch.test"),
        new("TrailRay", "ray@demo.tutumuch.test"),
        new("DenimDev", "dev@demo.tutumuch.test"),
        new("OxfordOwen", "owen@demo.tutumuch.test"),
        new("CoatRackCal", "cal@demo.tutumuch.test"),
    ];

    private const string Oxford = "Shoes/black-oxford.jpg";
    private const string Loafer = "Shoes/tan-penny-loafer.jpg";
    private const string PacBoot = "Shoes/black-pac-boot.jpg";
    private const string HikingBoot = "Shoes/brown-hiking-boot.jpg";
    private const string Chukka = "Shoes/navy-suede-chukka-sneaker.jpg";
    private const string TrailSneaker = "Shoes/grey-trail-sneaker.jpg";
    private const string FloralShirt = "Tops/floral-short-sleeve-shirt.jpg";
    private const string NavyPrintShirt = "Tops/navy-print-short-sleeve-shirt.jpg";
    private const string PlaidShirt = "Tops/plaid-button-down-shirt.jpg";
    private const string WhiteShirt = "Tops/white-dress-shirt.jpg";
    private const string Hoodie = "Tops/purple-hoodie.jpg";
    private const string PrintTee = "Tops/grey-print-t-shirt.jpg";
    private const string RainJacket = "Outerwear/blue-rain-jacket.jpg";
    private const string WorkJacket = "Outerwear/grey-work-jacket.jpg";
    private const string Anorak = "Outerwear/tan-pullover-anorak.jpg";
    private const string Overcoat = "Outerwear/olive-wool-overcoat.jpg";
    private const string Peacoat = "Outerwear/navy-peacoat.jpg";
    private const string MotoJacket = "Outerwear/black-leather-moto-jacket.jpg";
    private const string KhakiPants = "Bottoms/khaki-five-pocket-pants.jpg";
    private const string Chinos = "Bottoms/stone-chinos.jpg";
    private const string GreyJeans = "Bottoms/washed-grey-jeans.jpg";
    private const string LinenShorts = "Bottoms/grey-drawstring-shorts.jpg";
    private const string BoardShorts = "Bottoms/navy-board-shorts.jpg";
    private const string ChinoShorts = "Bottoms/orange-chino-shorts.jpg";

    private const string NewWithTags = "New with tags";
    private const string NewWithoutTags = "New without tags";
    private const string Excellent = "Used - Excellent";
    private const string Good = "Used - Good";
    private const string Fair = "Used - Fair";

    private static readonly IReadOnlyList<DemoListing> Shoes =
    [
        new("Shoes", "Dress shoes", "Allen Edmonds plain-toe oxfords, black", "Black calf leather oxfords with a slim profile. Resoled once, uppers polished and crease-free.", "Allen Edmonds", Excellent, null, 165m, 12m, [Oxford, Loafer]),
        new("Shoes", "Dress shoes", "Black leather derby shoes, worn twice", "Minimal black derbies with a leather sole. Worn to two weddings and stored in their box since.", "Everlane", Excellent, null, 95m, 10m, [Oxford]),
        new("Shoes", "Dress shoes", "Allen Edmonds tan penny loafers", "Cognac penny loafers with a hand-sewn moc toe. Tried on at home, never worn outside.", "Allen Edmonds", NewWithoutTags, null, 210m, 12m, [Loafer, Oxford]),
        new("Shoes", "Dress shoes", "Cognac leather loafers", "Easy slip-on loafers in a warm cognac leather. Light wear on the soles, uppers in good shape.", "Everlane", Good, null, 75m, 9m, [Loafer]),
        new("Shoes", "Boots", "Insulated duck boots, black canvas", "Waterproof rubber shell with a canvas upper and felt liner. Kept my feet dry through two winters.", "Carhartt", Good, null, 85m, 14m, [PacBoot, HikingBoot]),
        new("Shoes", "Boots", "Waterproof winter pac boots", "Tall lace-up pac boots with a removable liner and a lugged sole. Still have the tags and box.", "Barbour", NewWithTags, null, 140m, 15m, [PacBoot]),
        new("Shoes", "Boots", "Red Wing waterproof work boots", "Full-grain leather work boots with a padded collar and a lugged rubber sole.", "Red Wing", Fair, "Scuffed toe caps and a small crease across the left boot.", 120m, 15m, [HikingBoot, PacBoot, TrailSneaker]),
        new("Shoes", "Boots", "Adidas Terrex mid hiking boots", "Brown suede mid-height hikers with a waterproof membrane and a grippy outsole. About 50 trail miles.", "Adidas", Excellent, null, 95m, 12m, [HikingBoot]),
        new("Shoes", "Sneakers", "New Balance suede chukka sneakers, navy", "Navy suede chukka sneakers with a cushioned sole. Worn indoors once to check the fit.", "New Balance", NewWithoutTags, null, 88m, 10m, [Chukka, TrailSneaker]),
        new("Shoes", "Sneakers", "Navy suede high-top sneakers", "Soft navy suede high-tops with contrast stitching. Some creasing at the toe box.", "Nike", Good, null, 55m, 8m, [Chukka]),
        new("Shoes", "Sneakers", "Nike trail running shoes, grey", "Breathable grey mesh trail runners with a rock plate. Clean and odour-free.", "Nike", Excellent, null, 70m, 9m, [TrailSneaker, Chukka, HikingBoot]),
        new("Shoes", "Sneakers", "New Balance trail sneakers", "Lightweight trail sneakers in grey and lime. Brand new in the box, free shipping.", "New Balance", NewWithTags, null, 110m, 0m, [TrailSneaker]),
    ];

    private static readonly IReadOnlyList<DemoListing> Tops =
    [
        new("Tops", "Shirts", "Black and white floral short-sleeve shirt", "Crisp cotton camp-collar shirt with a bold black floral print. Washed cold and hung dry.", "Uniqlo", Excellent, null, 28m, 5m, [FloralShirt, NavyPrintShirt]),
        new("Tops", "Shirts", "Floral print camp shirt, cotton", "Relaxed camp shirt in a black-and-cream botanical print. Tags still attached.", "Everlane", NewWithTags, null, 45m, 5m, [FloralShirt]),
        new("Tops", "Shirts", "Navy print short-sleeve button-up", "Navy cotton shirt with a small all-over print and a chest pocket. Washed once.", "Uniqlo", NewWithoutTags, null, 24m, 5m, [NavyPrintShirt, FloralShirt]),
        new("Tops", "Shirts", "Patagonia navy print shirt", "Breathable short-sleeve shirt for warm days, navy with a light print. Slight fading at the collar.", "Patagonia", Good, null, 38m, 6m, [NavyPrintShirt]),
        new("Tops", "Shirts", "Green plaid button-down shirt", "Brushed cotton plaid in green and navy with a button-down collar. Like new.", "Barbour", Excellent, null, 55m, 6m, [PlaidShirt, WhiteShirt]),
        new("Tops", "Shirts", "Flannel plaid work shirt", "Heavy flannel shirt in a dark plaid, cut roomy for layering over a tee.", "Carhartt", Fair, "Light fading on the collar and one replaced button.", 18m, 6m, [PlaidShirt]),
        new("Tops", "Shirts", "White dress shirt, long sleeve", "Bright white poplin dress shirt with a spread collar and barrel cuffs. Never worn.", "Everlane", NewWithoutTags, null, 40m, 5m, [WhiteShirt, PlaidShirt]),
        new("Tops", "Shirts", "Crisp white oxford shirt", "Everyday white oxford cloth shirt. Some softening from washing, no marks.", "Uniqlo", Good, null, 20m, 5m, [WhiteShirt]),
        new("Tops", "Hoodies", "Purple pullover hoodie", "Heavyweight purple fleece hoodie with a kangaroo pocket and a printed chest graphic.", "Champion", Good, null, 32m, 7m, [Hoodie, PrintTee]),
        new("Tops", "Hoodies", "Champion reverse weave hoodie, purple", "Reverse weave fleece that keeps its shape. Bought the wrong colour, tags on.", "Champion", NewWithTags, null, 58m, 7m, [Hoodie]),
        new("Tops", "T-shirts", "Grey graphic print T-shirt", "Soft grey tee with a printed vest-and-tie graphic. A conversation starter.", "Champion", Excellent, null, 18m, 4m, [PrintTee, Hoodie]),
        new("Tops", "T-shirts", "Trompe-l'oeil vest print tee", "Cotton tee with a hand-drawn waistcoat print. New, still folded from the shop.", "Uniqlo", NewWithTags, null, 22m, 4m, [PrintTee]),
    ];

    private static readonly IReadOnlyList<DemoListing> Outerwear =
    [
        new("Outerwear", "Jackets", "Arc'teryx waterproof rain shell, blue", "Two-tone blue and grey rain shell with a helmet-compatible hood and pit zips.", "Arc'teryx", Excellent, null, 260m, 12m, [RainJacket, Anorak]),
        new("Outerwear", "Jackets", "Patagonia Torrentshell rain jacket", "Packable waterproof jacket that stuffs into its own pocket. Light wear on the cuffs.", "Patagonia", Good, null, 95m, 10m, [RainJacket]),
        new("Outerwear", "Jackets", "Grey cotton field jacket", "Four-pocket cotton field jacket in a mid grey. Softened with wear, all buttons present.", "Carhartt", Good, null, 65m, 10m, [WorkJacket, RainJacket]),
        new("Outerwear", "Jackets", "Grey four-pocket utility jacket", "Structured utility jacket with button-flap pockets and a stand collar. Unworn.", "Barbour", NewWithoutTags, null, 150m, 12m, [WorkJacket]),
        new("Outerwear", "Jackets", "Tan fleece pullover anorak", "Warm half-zip pullover in coyote tan with a kangaroo pocket. Great as a mid layer.", "Patagonia", Excellent, null, 70m, 9m, [Anorak, WorkJacket]),
        new("Outerwear", "Jackets", "Half-zip pullover, coyote tan", "Light fleece-lined pullover with a zipped chest pocket and elastic cuffs.", "Arc'teryx", Fair, "Pilling on the cuffs and a faint mark on the back.", 45m, 9m, [Anorak]),
        new("Outerwear", "Coats", "Olive wool overcoat, double-breasted", "Heavy olive wool overcoat with a wide collar, cut long to cover a jacket.", "Barbour", Excellent, null, 220m, 15m, [Overcoat, Peacoat]),
        new("Outerwear", "Coats", "Heavy wool greatcoat, olive", "Vintage-style greatcoat in dense olive wool with brass-tone buttons.", "Everlane", Good, null, 140m, 15m, [Overcoat]),
        new("Outerwear", "Coats", "Navy wool peacoat", "Classic double-breasted peacoat in navy melton wool. Tags attached.", "Everlane", NewWithTags, null, 180m, 14m, [Peacoat, Overcoat]),
        new("Outerwear", "Coats", "Double-breasted peacoat, charcoal navy", "Warm peacoat with a quilted lining and slash pockets. Worn one season.", "Uniqlo", Good, null, 75m, 14m, [Peacoat]),
        new("Outerwear", "Jackets", "Black leather motorcycle jacket", "Thick black leather racer jacket with striped sleeves and a quilted lining.", "Barbour", Excellent, null, 420m, 15m, [MotoJacket, RainJacket, WorkJacket, Anorak]),
        new("Outerwear", "Jackets", "Striped-sleeve leather racer jacket", "Broken-in leather racer with red, white and blue sleeve stripes.", "Carhartt", Fair, "Creasing at the elbows and a worn zip pull.", 210m, 15m, [MotoJacket]),
    ];

    private static readonly IReadOnlyList<DemoListing> Bottoms =
    [
        new("Bottoms", "Pants", "Khaki five-pocket canvas pants", "Durable khaki canvas pants with a relaxed leg. Washed many times, still sturdy.", "Carhartt", Good, null, 35m, 8m, [KhakiPants, Chinos]),
        new("Bottoms", "Pants", "Relaxed khaki work pants", "Five-pocket work pants in a light khaki duck. New with tags.", "Carhartt", NewWithTags, null, 55m, 8m, [KhakiPants]),
        new("Bottoms", "Pants", "Stone slim chinos", "Slim cotton chinos in stone with a little stretch. Hemmed once, never worn.", "Everlane", NewWithoutTags, null, 48m, 7m, [Chinos, KhakiPants]),
        new("Bottoms", "Pants", "Stretch chinos, stone", "Everyday stretch chinos in a pale stone colour. No stains or wear marks.", "Uniqlo", Excellent, null, 22m, 6m, [Chinos]),
        new("Bottoms", "Pants", "Washed grey jeans, relaxed fit", "Relaxed grey denim with a soft washed finish and a straight leg.", "Uniqlo", Good, null, 30m, 7m, [GreyJeans, KhakiPants]),
        new("Bottoms", "Pants", "Worn-in grey denim", "Properly broken-in grey jeans with honest wear and lots of character.", "Carhartt", Fair, "Fraying at the hems and fading on the knees.", 20m, 7m, [GreyJeans]),
        new("Bottoms", "Shorts", "Grey linen drawstring shorts", "Breathable linen shorts with a drawstring waist and patterned cuffs.", "Everlane", NewWithTags, null, 38m, 5m, [LinenShorts, BoardShorts]),
        new("Bottoms", "Shorts", "Linen-blend shorts with print cuffs", "Relaxed linen-blend shorts for hot days, grey with a printed turn-up.", "Uniqlo", Excellent, null, 19m, 5m, [LinenShorts]),
        new("Bottoms", "Shorts", "Navy board shorts with red waistband", "Quick-dry navy board shorts with a lace-up red waistband.", "Patagonia", Good, null, 28m, 5m, [BoardShorts, ChinoShorts]),
        new("Bottoms", "Shorts", "Patagonia Wavefarer board shorts, navy", "Stretch board shorts that work in and out of the water. Never worn.", "Patagonia", NewWithoutTags, null, 45m, 5m, [BoardShorts]),
        new("Bottoms", "Shorts", "Orange chino shorts", "Flat-front chino shorts in a burnt orange twill with slant pockets.", "Everlane", Excellent, null, 24m, 5m, [ChinoShorts, LinenShorts, BoardShorts]),
        new("Bottoms", "Shorts", "Burnt orange flat-front shorts", "Bright summer shorts in burnt orange cotton. New with tags, free shipping.", "Uniqlo", NewWithTags, null, 25m, 0m, [ChinoShorts]),
    ];

    /// <summary>
    /// Every listing in publish order, oldest first. The categories alternate, so the newest
    /// listings on the home page mix shoes and clothing, and the two listings that share a cover
    /// photo are published six apart instead of side by side.
    /// </summary>
    public static readonly IReadOnlyList<DemoListing> Listings = Interleave(Spread(Shoes), Spread(Tops), Spread(Outerwear), Spread(Bottoms));

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
