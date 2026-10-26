namespace TuTuMuch.Cli;

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
/// The fixed demo set (seed-demo-data design): 8 members and 72 listings, 12 in each of the
/// 6 categories. Brands
/// come from Selling:SuggestedBrands; photos are the committed original illustrations in
/// SeedPhotos/. A small demo, not a load or scale fixture (ADR general/0002).
/// </summary>
public static class DemoCatalogue
{
    /// <summary>The shared demo password, documented in the CLI README. For local use only.</summary>
    public const string Password = "TuTuMuch-demo-only-2026";

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

    private const string PartyDress = "Clothing/pink-floral-party-dress.jpg";
    private const string Sundress = "Clothing/yellow-gingham-sundress.jpg";
    private const string TutuSkirt = "Clothing/pink-tulle-tutu-skirt.jpg";
    private const string PleatedSkirt = "Clothing/lavender-pleated-skirt.jpg";
    private const string Romper = "Clothing/mint-ruffle-romper.jpg";
    private const string TutuDress = "Clothing/lavender-tutu-dress.jpg";
    private const string FloralBlouse = "Clothing/floral-peter-pan-blouse.jpg";
    private const string RuffleBlouse = "Clothing/mint-ruffle-blouse.jpg";
    private const string RainbowSweater = "Clothing/rainbow-knit-sweater.jpg";
    private const string Cardigan = "Clothing/cream-heart-cardigan.jpg";
    private const string Hoodie = "Clothing/lavender-heart-hoodie.jpg";
    private const string UnicornTee = "Clothing/unicorn-graphic-t-shirt.jpg";
    private const string Raincoat = "Clothing/yellow-raincoat.jpg";
    private const string DenimJacket = "Clothing/light-wash-denim-jacket.jpg";
    private const string FleeceJacket = "Clothing/mint-fleece-jacket.jpg";
    private const string WoolCoat = "Clothing/pink-wool-coat.jpg";
    private const string DuffleCoat = "Clothing/red-duffle-coat.jpg";
    private const string PufferJacket = "Clothing/purple-puffer-jacket.jpg";
    private const string CordPants = "Clothing/pink-corduroy-pants.jpg";
    private const string DenimLeggings = "Clothing/denim-leggings.jpg";
    private const string Jeans = "Clothing/light-wash-jeans.jpg";
    private const string FloralShorts = "Clothing/floral-shorts.jpg";
    private const string DenimShorts = "Clothing/denim-shorts.jpg";
    private const string BikeShorts = "Clothing/yellow-bike-shorts.jpg";
    private const string CanvasSneakers = "Shoes/pink-canvas-sneakers.jpg";
    private const string HighTops = "Shoes/lavender-high-top-sneakers.jpg";
    private const string RainBoots = "Shoes/yellow-rain-boots.jpg";
    private const string AnkleBoots = "Shoes/brown-ankle-boots.jpg";
    private const string Sandals = "Shoes/mint-strap-sandals.jpg";
    private const string MaryJanes = "Shoes/red-mary-janes.jpg";
    private const string BalletTutu = "Dance/white-ballet-tutu.jpg";
    private const string LavenderLeotard = "Dance/lavender-leotard.jpg";
    private const string SequinLeotard = "Dance/teal-sequin-leotard.jpg";
    private const string BalletSlippers = "Dance/pink-ballet-slippers.jpg";
    private const string TapShoes = "Dance/black-tap-shoes.jpg";
    private const string DanceBag = "Dance/pink-dance-bag.jpg";
    private const string SoccerBall = "Sports/soccer-ball.jpg";
    private const string Volleyball = "Sports/volleyball.jpg";
    private const string GymLeotard = "Sports/purple-gymnastics-leotard.jpg";
    private const string Goggles = "Sports/swim-goggles.jpg";
    private const string Scooter = "Sports/pink-kick-scooter.jpg";
    private const string BalanceBike = "Sports/mint-balance-bike.jpg";
    private const string MintCamera = "Cameras and tech/mint-instant-camera.jpg";
    private const string PinkCamera = "Cameras and tech/pink-instant-camera.jpg";
    private const string KidsCamera = "Cameras and tech/yellow-kids-camera.jpg";
    private const string LavenderHeadphones = "Cameras and tech/lavender-headphones.jpg";
    private const string PinkHeadphones = "Cameras and tech/pink-headphones.jpg";
    private const string TabletCase = "Cameras and tech/purple-tablet-case.jpg";
    private const string RagDoll = "Toys and crafts/rag-doll.jpg";
    private const string PaintSet = "Toys and crafts/watercolour-paint-set.jpg";
    private const string Puzzle = "Toys and crafts/jigsaw-puzzle.jpg";
    private const string BoardGame = "Toys and crafts/board-game.jpg";
    private const string PictureBooks = "Toys and crafts/picture-books.jpg";
    private const string Crayons = "Toys and crafts/crayon-box.jpg";

    private const string NewWithTags = "New with tags";
    private const string NewWithoutTags = "New without tags";
    private const string Excellent = "Used - Excellent";
    private const string Good = "Used - Good";
    private const string Fair = "Used - Fair";

    private const string ClothingCategory = "Clothing";
    private const string ShoesCategory = "Shoes";
    private const string DanceCategory = "Dance";
    private const string SportsCategory = "Sports";
    private const string TechCategory = "Cameras and tech";
    private const string ToysCategory = "Toys and crafts";

    private static readonly IReadOnlyList<DemoListing> Clothing =
    [
        new(ClothingCategory, "Dresses", "Floral party dress, pink", "Pink cotton party dress with a white floral print, puff sleeves and a sash that ties at the back. Worn to one birthday party.", "Janie and Jack", Excellent, null, 38m, 6m, [PartyDress, TutuDress]),
        new(ClothingCategory, "Dresses", "Yellow gingham sundress", "Sunny yellow gingham sundress with wide straps and a smocked bodice. Tried on at home, never worn out.", "Hanna Andersson", NewWithoutTags, null, 34m, 6m, [Sundress, Romper]),
        new(ClothingCategory, "Dresses", "Pink tulle tutu skirt", "Three layers of soft pink tulle on a comfy elastic waist. Lots of twirl left after a season of parties.", "Carter's", Good, null, 14m, 5m, [TutuSkirt, PleatedSkirt]),
        new(ClothingCategory, "Tops", "Floral Peter Pan collar blouse", "Cotton blouse with a sweet floral print and a rounded Peter Pan collar. Washed cold and hung dry.", "Mini Boden", Excellent, null, 16m, 4m, [FloralBlouse, RuffleBlouse]),
        new(ClothingCategory, "Tops", "Rainbow knit sweater", "Chunky knit sweater striped in every colour of the rainbow. Soft and cosy.", "Tea Collection", Fair, "Light pilling on the sleeves and one small mended spot at the cuff.", 9m, 4m, [RainbowSweater, Cardigan]),
        new(ClothingCategory, "Tops", "Lavender heart hoodie", "Fleece-lined lavender hoodie with a kangaroo pocket and a big printed heart. Tags still on.", "Old Navy", NewWithTags, null, 22m, 5m, [Hoodie, UnicornTee]),
        new(ClothingCategory, "Outerwear", "Yellow rain jacket with hood", "Bright yellow waterproof rain jacket with a peaked hood and reflective trim. Kept her dry all spring.", "Hanna Andersson", Excellent, null, 36m, 7m, [Raincoat, DenimJacket]),
        new(ClothingCategory, "Outerwear", "Mint fleece zip jacket", "Warm mint fleece jacket with zipped pockets. Great as a layer under a raincoat.", "Carter's", Good, null, 14m, 5m, [FleeceJacket, PufferJacket]),
        new(ClothingCategory, "Outerwear", "Red duffle coat with toggles", "Classic red duffle coat with wooden toggles and a cosy hood. Tags attached.", "Mini Boden", NewWithTags, null, 60m, 8m, [DuffleCoat, WoolCoat]),
        new(ClothingCategory, "Bottoms", "Pink corduroy pants", "Soft pink corduroy pants with an elastic waist and roll-up cuffs. Washed many times, still sturdy.", "OshKosh B'gosh", Good, null, 10m, 5m, [CordPants, DenimLeggings]),
        new(ClothingCategory, "Bottoms", "Worn-in light-wash jeans", "Properly playground-tested jeans with honest wear and lots of life left.", "Gap Kids", Fair, "Fraying at the hems and fading on the knees.", 5m, 5m, [Jeans, DenimShorts]),
        new(ClothingCategory, "Bottoms", "Floral cotton shorts", "Breathable cotton shorts in a bright floral print with a drawstring waist. New, free shipping.", "Carter's", NewWithTags, null, 12m, 0m, [FloralShorts, BikeShorts]),
    ];

    private static readonly IReadOnlyList<DemoListing> Shoes =
    [
        new(ShoesCategory, "Sneakers", "Pink canvas sneakers", "Low-top canvas sneakers in candy pink with white laces. Worn a handful of times.", "Converse Kids", Excellent, null, 22m, 6m, [CanvasSneakers, HighTops]),
        new(ShoesCategory, "Sneakers", "Velcro play sneakers, blush", "Easy hook-and-loop sneakers she can put on herself. Scuffed toes from the playground.", "Stride Rite", Fair, "Scuffs on both toes and a little fraying on one strap.", 8m, 6m, [CanvasSneakers]),
        new(ShoesCategory, "Sneakers", "Lavender high-top sneakers", "Lilac high-tops with a star patch on the ankle. Still in the box.", "Converse Kids", NewWithTags, null, 38m, 7m, [HighTops, CanvasSneakers]),
        new(ShoesCategory, "Sneakers", "Star high-tops, lilac", "Comfy lilac high-tops with a padded collar. Light creasing, clean soles.", "Stride Rite", Good, null, 16m, 6m, [HighTops]),
        new(ShoesCategory, "Boots", "Yellow rain boots", "Sunny yellow rubber rain boots with easy pull-on handles. Made for puddles.", "Hunter Kids", Excellent, null, 28m, 8m, [RainBoots, AnkleBoots]),
        new(ShoesCategory, "Boots", "Puddle boots with flower buttons", "Bright yellow puddle boots with little flower details. Worn one wet season.", "Native", Good, null, 14m, 7m, [RainBoots]),
        new(ShoesCategory, "Boots", "Tan lace-up ankle boots", "Soft tan ankle boots with a side zip and laces for show. Never worn outside.", "Stride Rite", NewWithoutTags, null, 42m, 8m, [AnkleBoots, RainBoots]),
        new(ShoesCategory, "Boots", "Faux-suede ankle boots, camel", "Camel ankle boots with a cosy lining. Some scuffing on the heels.", "Old Navy", Fair, "Scuffing on both heels and a faint water mark on one toe.", 7m, 7m, [AnkleBoots]),
        new(ShoesCategory, "Sandals", "Mint strap sandals", "Mint sandals with three adjustable straps and soft footbeds. Worn on one holiday.", "Native", Excellent, null, 18m, 5m, [Sandals, MaryJanes]),
        new(ShoesCategory, "Sandals", "Seafoam water sandals", "Quick-drying sandals for the beach and splash pad. New with tags, free shipping.", "Native", NewWithTags, null, 24m, 0m, [Sandals]),
        new(ShoesCategory, "Dress shoes", "Red patent Mary Janes", "Shiny red Mary Janes with a buckle strap, perfect for holidays. Worn to two parties.", "Janie and Jack", Excellent, null, 26m, 6m, [MaryJanes, Sandals]),
        new(ShoesCategory, "Dress shoes", "Cherry buckle party shoes", "Cherry red party shoes with a soft rubber sole. Tried on once.", "Stride Rite", NewWithoutTags, null, 30m, 6m, [MaryJanes]),
    ];

    private static readonly IReadOnlyList<DemoListing> Dance =
    [
        new(DanceCategory, "Tutus", "White ballet tutu", "Layered white tutu with a pink bodice and a satin waistband. Worn for one recital.", "Capezio", Excellent, null, 34m, 7m, [BalletTutu, LavenderLeotard]),
        new(DanceCategory, "Tutus", "Recital tutu, snow white", "Stiff-netted recital tutu with tiny pearl details. Some flattening in the net.", "Bloch", Fair, "The top layer of net is a little flattened and one pearl is missing.", 15m, 7m, [BalletTutu]),
        new(DanceCategory, "Leotards", "Lavender tank leotard", "Classic lavender tank leotard in soft cotton stretch. Ready for ballet class.", "Capezio", Good, null, 12m, 4m, [LavenderLeotard, BalletSlippers]),
        new(DanceCategory, "Leotards", "Camisole leotard, lilac", "Lilac camisole leotard with a scooped back. New, still in its packet.", "Bloch", NewWithTags, null, 26m, 4m, [LavenderLeotard]),
        new(DanceCategory, "Leotards", "Teal sequin leotard", "Sparkly teal leotard with sequins across the front. Worn for one show.", "Capezio", Excellent, null, 28m, 5m, [SequinLeotard, BalletTutu]),
        new(DanceCategory, "Leotards", "Sparkle stage leotard, mint", "Mint stage leotard with scattered sparkles and a soft lining. Never worn.", "Danskin", NewWithoutTags, null, 30m, 5m, [SequinLeotard]),
        new(DanceCategory, "Dance shoes", "Pink leather ballet slippers", "Full-sole leather ballet slippers with elastics sewn on. A term of classes in them.", "Bloch", Good, null, 10m, 4m, [BalletSlippers, DanceBag]),
        new(DanceCategory, "Dance shoes", "Canvas split-sole ballet shoes", "Pink canvas split-sole slippers, still with their drawstrings. New with tags.", "Capezio", NewWithTags, null, 22m, 4m, [BalletSlippers]),
        new(DanceCategory, "Dance shoes", "Black patent tap shoes", "Black patent tap shoes with a ribbon tie and good taps. Light creasing.", "Capezio", Good, null, 20m, 6m, [TapShoes, BalletSlippers]),
        new(DanceCategory, "Dance shoes", "Buckle tap shoes, black", "Well-used tap shoes with lots of sound left in the taps.", "Bloch", Fair, "Scratches on the patent and worn heel taps.", 9m, 6m, [TapShoes]),
        new(DanceCategory, "Dance bags", "Pink ballerina dance bag", "Roomy pink dance bag with a ballerina patch and a zip pocket for slippers.", "Capezio", Excellent, null, 18m, 5m, [DanceBag, TapShoes]),
        new(DanceCategory, "Dance bags", "Blush duffel dance bag", "Blush pink duffel with a shoe compartment. New with tags, free shipping.", "Bloch", NewWithTags, null, 32m, 0m, [DanceBag]),
    ];

    private static readonly IReadOnlyList<DemoListing> Sports =
    [
        new(SportsCategory, "Soccer", "Size 4 soccer ball", "Size 4 match ball, holds air well. Used for one season of club practice.", "Adidas Kids", Good, null, 12m, 6m, [SoccerBall, Volleyball]),
        new(SportsCategory, "Soccer", "Training soccer ball, size 3", "Soft-touch size 3 ball for little players. Never kicked.", "Nike Kids", NewWithTags, null, 18m, 6m, [SoccerBall]),
        new(SportsCategory, "Volleyball", "Soft-touch youth volleyball", "Lightweight youth volleyball that is easy on small hands. Barely used.", "Mikasa", Excellent, null, 20m, 6m, [Volleyball, SoccerBall]),
        new(SportsCategory, "Volleyball", "Beach volleyball, sunshine", "Bright beach volleyball for the summer. Scuffed from the sand, still round.", "Mikasa", Fair, "Scuffing across the panels and faded colours.", 6m, 5m, [Volleyball]),
        new(SportsCategory, "Gymnastics", "Purple gymnastics leotard", "Purple gymnastics leotard with a pink V and sparkles. Worn at two meets.", "GK Elite", Excellent, null, 36m, 5m, [GymLeotard, Goggles]),
        new(SportsCategory, "Gymnastics", "Sparkle competition leotard, plum", "Plum competition leotard with a mesh V. Never worn, tags on.", "GK Elite", NewWithTags, null, 52m, 5m, [GymLeotard]),
        new(SportsCategory, "Swimming", "Aqua swim goggles", "Anti-fog junior goggles with a split strap. Used for one season of lessons.", "Speedo", Good, null, 6m, 3m, [Goggles, GymLeotard]),
        new(SportsCategory, "Swimming", "Junior goggles, mirrored lenses", "Junior goggles in an aqua frame. New in packet, free shipping.", "Speedo", NewWithTags, null, 14m, 0m, [Goggles]),
        new(SportsCategory, "Bikes and scooters", "Pink kick scooter", "Three-wheel kick scooter in pink and purple with smooth wheels and a height-adjustable bar.", "Micro Kickboard", Excellent, null, 65m, 12m, [Scooter, BalanceBike]),
        new(SportsCategory, "Bikes and scooters", "Folding scooter, berry", "Well-used folding scooter that still rolls straight.", "Razor", Fair, "Worn handle grips and scratches on the deck.", 18m, 12m, [Scooter]),
        new(SportsCategory, "Bikes and scooters", "Mint balance bike", "Lightweight mint balance bike with air tyres and an adjustable seat.", "Strider", Good, null, 55m, 15m, [BalanceBike, Scooter]),
        new(SportsCategory, "Bikes and scooters", "Balance bike with pink seat", "Balance bike assembled and ridden once around the block.", "Strider", NewWithoutTags, null, 85m, 15m, [BalanceBike]),
    ];

    private static readonly IReadOnlyList<DemoListing> CamerasAndTech =
    [
        new(TechCategory, "Cameras", "Mint instant camera", "Instant camera in mint with a built-in selfie mirror. Takes mini film. Works perfectly.", "Fujifilm Instax", Excellent, null, 48m, 8m, [MintCamera, PinkCamera]),
        new(TechCategory, "Cameras", "Instant camera bundle, seafoam", "Seafoam instant camera with a strap and an album. New in box.", "Fujifilm Instax", NewWithTags, null, 75m, 8m, [MintCamera]),
        new(TechCategory, "Cameras", "Blossom pink instant camera", "Pink instant print camera that prints straight away. Light scratches on the back.", "Kodak", Good, null, 34m, 7m, [PinkCamera, MintCamera]),
        new(TechCategory, "Cameras", "Print camera, bubblegum", "Pink print camera with zero-ink paper. Flash cover is cracked but works.", "Kodak", Fair, "Small crack in the flash cover and scuffs on the corners.", 18m, 7m, [PinkCamera]),
        new(TechCategory, "Cameras", "Yellow kids' digital camera", "Chunky shockproof kids' camera with two viewfinders and games. Includes the cable.", "VTech", Excellent, null, 28m, 7m, [KidsCamera, TabletCase]),
        new(TechCategory, "Cameras", "Kids' selfie camera, sunshine", "Easy-grip kids' camera that takes photos and videos. Never used.", "VTech", NewWithoutTags, null, 38m, 7m, [KidsCamera]),
        new(TechCategory, "Headphones", "Lavender kids' headphones", "Volume-limited headphones with soft cushions and a fold-flat band.", "JLab", Good, null, 12m, 5m, [LavenderHeadphones, PinkHeadphones]),
        new(TechCategory, "Headphones", "Wireless kids' headphones, lilac", "Lilac wireless headphones with an 85 dB volume limit. New with tags.", "JLab", NewWithTags, null, 30m, 5m, [LavenderHeadphones]),
        new(TechCategory, "Headphones", "Pink headphones with star buttons", "Pink wired headphones with little star buttons. Worn on a few flights.", "JLab", Excellent, null, 14m, 5m, [PinkHeadphones, LavenderHeadphones]),
        new(TechCategory, "Headphones", "Wired headphones, blush", "Blush headphones that have been well loved. Sound is still clear.", "Kodak", Fair, "Cushions are worn and the band has light scratches.", 6m, 5m, [PinkHeadphones]),
        new(TechCategory, "Tablet accessories", "Purple kid-proof tablet case", "Chunky foam tablet case with a handle that doubles as a stand. Fits most 10-inch tablets.", "JLab", Good, null, 10m, 5m, [TabletCase, KidsCamera]),
        new(TechCategory, "Tablet accessories", "Grape foam tablet case with handle", "Grape foam case with a carry handle. New, free shipping.", "Fintie", NewWithTags, null, 22m, 0m, [TabletCase]),
    ];

    private static readonly IReadOnlyList<DemoListing> ToysAndCrafts =
    [
        new(ToysCategory, "Dolls", "Soft rag doll in a pink dress", "Cuddly rag doll with yarn pigtails and a pink cotton dress. A gentle bedtime friend.", "Melissa & Doug", Excellent, null, 16m, 5m, [RagDoll, PictureBooks]),
        new(ToysCategory, "Dolls", "Rag doll, rosy cheeks", "Well-loved rag doll who has been on many adventures.", "Melissa & Doug", Fair, "Some fuzzing on the dress and one loose stitch on her hand.", 5m, 5m, [RagDoll]),
        new(ToysCategory, "Arts and crafts", "Watercolour paint set", "Six-colour watercolour palette with a brush. Only the pink and blue have been used.", "Crayola", Good, null, 6m, 3m, [PaintSet, Crayons]),
        new(ToysCategory, "Arts and crafts", "Washable paint set with brush", "Washable watercolour set, still sealed. Great for rainy days.", "Crayola", NewWithTags, null, 9m, 3m, [PaintSet]),
        new(ToysCategory, "Arts and crafts", "Big box of crayons", "Bright crayons in a flip-top box, every colour still there.", "Crayola", Excellent, null, 5m, 3m, [Crayons, PaintSet]),
        new(ToysCategory, "Arts and crafts", "Twistable crayons, 24 pack", "Twistable crayons that never need sharpening. Opened, never used. Free shipping.", "Crayola", NewWithoutTags, null, 8m, 0m, [Crayons]),
        new(ToysCategory, "Games and puzzles", "Four-piece floor puzzle", "Chunky pastel floor puzzle with big easy pieces. All pieces present.", "Melissa & Doug", Good, null, 8m, 5m, [Puzzle, BoardGame]),
        new(ToysCategory, "Games and puzzles", "Pastel jigsaw, 100 pieces", "100-piece jigsaw in pastel colours. Done twice, every piece counted.", "Ravensburger", Excellent, null, 10m, 5m, [Puzzle]),
        new(ToysCategory, "Games and puzzles", "Race-to-the-finish board game", "Colourful family board game with dice and counters. Box corners are a little worn.", "Hasbro", Good, null, 12m, 7m, [BoardGame, Puzzle]),
        new(ToysCategory, "Games and puzzles", "Brick building set, pastel cafe", "Pastel brick cafe set, complete with instructions. Built once and boxed again.", "LEGO", Excellent, null, 30m, 7m, [BoardGame]),
        new(ToysCategory, "Books", "Stack of five picture books", "Five bedtime picture books about friendship and adventures. Spines intact.", "Scholastic", Good, null, 12m, 6m, [PictureBooks, RagDoll]),
        new(ToysCategory, "Books", "First readers bundle", "Early reader bundle with a few crayon marks inside.", "Scholastic", Fair, "Crayon marks on two pages and a bent cover corner.", 6m, 6m, [PictureBooks]),
    ];

    /// <summary>
    /// Every listing in publish order, oldest first. The categories alternate, so the newest
    /// listings on the home page mix all 6 categories, and the two listings that share a cover
    /// photo are published six apart instead of side by side.
    /// </summary>
    public static readonly IReadOnlyList<DemoListing> Listings =
        Interleave(Spread(Clothing), Spread(Shoes), Spread(Dance), Spread(Sports), Spread(CamerasAndTech), Spread(ToysAndCrafts));

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
