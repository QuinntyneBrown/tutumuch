"""Cast and catalog shared by every mock. Today is Sun 25 Oct 2026.

Reuse these people, items and orders everywhere so a reviewer can follow one
story across screens. Never invent a new person or item in a screen module;
add it here first.
"""

TODAY = "Sun 25 Oct 2026"
FEE_PERCENT = 10

# Display names are public. Real names appear only to the member themself
# and on a seller's shipping label. Members are parents and families selling
# what their daughters have outgrown.
PEOPLE = {
    "TutuTessa": {"name": "Tessa Bell", "initials": "TB", "city": "Brooklyn, NY", "since": "Mar 2025", "rating": 4.8, "reviews": 23, "sales": 19, "email": "tessa.bell@example.com", "bio": "Mom of two girls. Selling what they’ve outgrown, buying the next size up."},
    "TwirlyTara": {"name": "Tara Okafor", "initials": "TO", "city": "Atlanta, GA", "since": "Jan 2025", "rating": 4.9, "reviews": 128, "sales": 141, "bio": "Party dresses and tutus, washed and pressed. Real photos, ships next day."},
    "HandMeDownHana": {"name": "Hana Ferreira", "initials": "HF", "city": "Portland, OR", "since": "Jun 2025", "rating": 4.7, "reviews": 41, "sales": 46, "bio": "Three sisters’ worth of coats and knits. Everything measured flat."},
    "PuddleJumperPia": {"name": "Pia Reyes", "initials": "PR", "city": "Denver, CO", "since": "Aug 2026", "rating": 5.0, "reviews": 2, "sales": 0, "bio": ""},
    "RainbowRosa": {"name": "Rosa Kim", "initials": "RK", "city": "Chicago, IL", "since": "Oct 2026", "rating": None, "reviews": 0, "sales": 0, "bio": "Clearing out the wardrobe."},
}
ME = "TutuTessa"          # the signed-in member in member mocks
ADMIN = {"name": "Ana Ruiz", "initials": "AR", "handle": "ana.admin"}

# Every item is for girls; the one accepted size is Girls' in every category (an audience lock).
# tone = photo backdrop 1..6, img = assets/items/<img>.svg
ITEMS = {
    "pink-tutu": {"id": "L-20931", "title": "Pink tulle tutu skirt", "brand": "Janie and Jack", "cat": "Dance", "sub": "Tutus", "cond": "Used - Excellent", "price": 32, "ship": 6, "seller": "TwirlyTara", "img": "tutu", "tone": 6, "listed": "2 hours ago", "photos": 5},
    "party-dress": {"id": "L-20877", "title": "Floral party dress", "brand": "Mini Boden", "cat": "Clothing", "sub": "Dresses", "cond": "Used - Good", "price": 42, "ship": 7, "seller": "HandMeDownHana", "img": "party-dress", "tone": 1, "listed": "Yesterday", "photos": 6},
    "rainbow-romper": {"id": "L-20902", "title": "Rainbow stripe romper", "brand": "Tea Collection", "cat": "Clothing", "sub": "Dresses", "cond": "New with tags", "price": 24, "ship": 5, "seller": "RainbowRosa", "img": "romper", "tone": 2, "listed": "5 hours ago", "photos": 4},
    "velvet-dress": {"id": "L-20915", "title": "Velvet holiday dress", "brand": "Janie and Jack", "cat": "Clothing", "sub": "Dresses", "cond": "New with tags", "price": 58, "ship": 0, "seller": "TwirlyTara", "img": "holiday-dress", "tone": 4, "listed": "3 hours ago", "photos": 5},
    "pleated-skirt": {"id": "L-20811", "title": "Pleated plaid skirt", "brand": "Zara Kids", "cat": "Clothing", "sub": "Bottoms", "cond": "Used - Excellent", "price": 26, "ship": 5, "seller": "TwirlyTara", "img": "skirt", "tone": 3, "listed": "3 days ago", "photos": 6},
    "sundress": {"id": "L-20790", "title": "Yellow gingham sundress", "brand": "Carter's", "cat": "Clothing", "sub": "Dresses", "cond": "Used - Fair", "price": 12, "ship": 5, "seller": "TutuTessa", "img": "sundress", "tone": 5, "listed": "4 days ago", "photos": 3, "flaws": "Small mark on the hem. One shoulder tie re-stitched."},
    "rainbow-sweater": {"id": "L-20866", "title": "Rainbow knit sweater", "brand": "Mini Boden", "cat": "Clothing", "sub": "Tops", "cond": "Used - Excellent", "price": 32, "ship": 5, "seller": "HandMeDownHana", "img": "sweater", "tone": 1, "listed": "Yesterday", "photos": 4},
    "heart-hoodie": {"id": "L-20855", "title": "Pink heart hoodie", "brand": "Gap Kids", "cat": "Clothing", "sub": "Tops", "cond": "Used - Good", "price": 38, "ship": 7, "seller": "TutuTessa", "img": "hoodie", "tone": 6, "listed": "2 days ago", "photos": 3},
    "collar-blouse": {"id": "L-20921", "title": "Peter Pan collar blouse", "brand": "Zara Kids", "cat": "Clothing", "sub": "Tops", "cond": "New with tags", "price": 18, "ship": 5, "seller": "RainbowRosa", "img": "blouse", "tone": 2, "listed": "4 hours ago", "photos": 2},
    "unicorn-tee": {"id": "L-20899", "title": "Unicorn graphic T-shirt", "brand": "H&M Kids", "cat": "Clothing", "sub": "Tops", "cond": "New without tags", "price": 12, "ship": 5, "seller": "RainbowRosa", "img": "tee", "tone": 3, "listed": "6 hours ago", "photos": 2},
    "puffer-jacket": {"id": "L-20840", "title": "Purple puffer jacket", "brand": "Old Navy", "cat": "Clothing", "sub": "Outerwear", "cond": "Used - Good", "price": 34, "ship": 8, "seller": "HandMeDownHana", "img": "puffer", "tone": 5, "listed": "2 days ago", "photos": 6},
    "raincoat": {"id": "L-20802", "title": "Yellow raincoat", "brand": "Hanna Andersson", "cat": "Clothing", "sub": "Outerwear", "cond": "Used - Good", "price": 44, "ship": 8, "seller": "HandMeDownHana", "img": "raincoat", "tone": 2, "listed": "4 days ago", "photos": 7, "status": "Reserved"},
    "denim-leggings": {"id": "L-20870", "title": "Denim leggings", "brand": "OshKosh B'gosh", "cat": "Clothing", "sub": "Bottoms", "cond": "New without tags", "price": 16, "ship": 5, "seller": "TwirlyTara", "img": "leggings", "tone": 4, "listed": "Yesterday", "photos": 4},
    "floral-shorts": {"id": "L-20760", "title": "Floral cotton shorts", "brand": "Carter's", "cat": "Clothing", "sub": "Bottoms", "cond": "Used - Excellent", "price": 30, "ship": 7, "seller": "TutuTessa", "img": "shorts", "tone": 6, "listed": "6 days ago", "photos": 3},
    "glitter-sneakers": {"id": "L-20925", "title": "Glitter light-up sneakers", "brand": "Stride Rite", "cat": "Shoes", "sub": "Sneakers", "cond": "Used - Excellent", "price": 22, "ship": 6, "seller": "TwirlyTara", "img": "sneakers", "tone": 6, "listed": "3 hours ago", "photos": 4},
    "rain-boots": {"id": "L-20848", "title": "Polka dot rain boots", "brand": "Hatley", "cat": "Shoes", "sub": "Boots", "cond": "Used - Good", "price": 18, "ship": 7, "seller": "HandMeDownHana", "img": "rain-boots", "tone": 2, "listed": "2 days ago", "photos": 3},
    "ballet-leotard": {"id": "L-20918", "title": "Lilac ballet leotard", "brand": "Capezio", "cat": "Dance", "sub": "Leotards", "cond": "New with tags", "price": 20, "ship": 5, "seller": "TwirlyTara", "img": "leotard", "tone": 4, "listed": "4 hours ago", "photos": 3},
    "ballet-slippers": {"id": "L-20861", "title": "Pink leather ballet slippers", "brand": "Bloch", "cat": "Dance", "sub": "Dance shoes", "cond": "Used - Excellent", "price": 14, "ship": 5, "seller": "TwirlyTara", "img": "ballet-slippers", "tone": 1, "listed": "Yesterday", "photos": 2},
    "soccer-ball": {"id": "L-20893", "title": "Size 4 soccer ball", "brand": "Adidas", "cat": "Sports", "sub": "Soccer", "cond": "Used - Good", "price": 15, "ship": 7, "seller": "PuddleJumperPia", "img": "soccer-ball", "tone": 3, "listed": "6 hours ago", "photos": 3},
    "kick-scooter": {"id": "L-20834", "title": "Purple three-wheel scooter", "brand": "Micro Kickboard", "cat": "Sports", "sub": "Bikes and scooters", "cond": "Used - Good", "price": 48, "ship": 12, "seller": "HandMeDownHana", "img": "scooter", "tone": 5, "listed": "2 days ago", "photos": 5},
    "instant-camera": {"id": "L-20909", "title": "Mint instant camera", "brand": "Fujifilm", "cat": "Cameras and tech", "sub": "Cameras", "cond": "Used - Excellent", "price": 45, "ship": 6, "seller": "RainbowRosa", "img": "camera", "tone": 2, "listed": "5 hours ago", "photos": 4},
    "cat-headphones": {"id": "L-20882", "title": "Cat-ear kids' headphones", "brand": "JLab", "cat": "Cameras and tech", "sub": "Headphones", "cond": "New without tags", "price": 19, "ship": 5, "seller": "RainbowRosa", "img": "headphones", "tone": 6, "listed": "Yesterday", "photos": 2},
    "rag-doll": {"id": "L-20876", "title": "Soft rag doll with yellow dress", "brand": "Melissa & Doug", "cat": "Toys and crafts", "sub": "Dolls", "cond": "Used - Excellent", "price": 16, "ship": 6, "seller": "TutuTessa", "img": "doll", "tone": 5, "listed": "Yesterday", "photos": 3},
    "paint-set": {"id": "L-20912", "title": "Watercolour paint set", "brand": "Crayola", "cat": "Toys and crafts", "sub": "Arts and crafts", "cond": "New with tags", "price": 11, "ship": 5, "seller": "RainbowRosa", "img": "paint-set", "tone": 3, "listed": "4 hours ago", "photos": 2},
}

# Catalog order, newest first (L2-022).
CATALOG = ["pink-tutu", "glitter-sneakers", "velvet-dress", "ballet-leotard", "instant-camera", "paint-set", "rainbow-romper", "soccer-ball",
           "cat-headphones", "party-dress", "rag-doll", "rainbow-sweater", "kick-scooter", "rain-boots", "raincoat", "sundress"]

# (category, subcategories, the one accepted size). The size is stored as "Girls'"
# and shown on size tags as "Girls".
CATEGORIES = [
    ("Clothing", ["Dresses", "Tops", "Bottoms", "Outerwear", "Pajamas", "Swimwear"], "Girls'"),
    ("Shoes", ["Sneakers", "Boots", "Sandals", "Dress shoes"], "Girls'"),
    ("Dance", ["Tutus", "Leotards", "Dance shoes", "Dance bags"], "Girls'"),
    ("Sports", ["Soccer", "Volleyball", "Gymnastics", "Swimming", "Bikes and scooters"], "Girls'"),
    ("Cameras and tech", ["Cameras", "Headphones", "Tablet accessories"], "Girls'"),
    ("Toys and crafts", ["Dolls", "Arts and crafts", "Games and puzzles", "Books"], "Girls'"),
]
CONDITIONS = ["New with tags", "New without tags", "Used - Excellent", "Used - Good", "Used - Fair"]
BRANDS = ["Carter's", "Capezio", "Stride Rite", "Crayola", "OshKosh B'gosh", "Gap Kids", "Old Navy", "Hanna Andersson", "Mini Boden", "Janie and Jack", "Tea Collection", "Zara Kids", "H&M Kids"]

# Orders. Totals: price + shipping + tax. Fee is 10% of item + shipping (L2-031).
ORDERS = {
    # Tessa's purchases
    "TM-48213": {"item": "party-dress", "buyer": "TutuTessa", "seller": "HandMeDownHana", "status": "Shipped", "date": "Thu 22 Oct", "tax": 4.35, "total": 53.35, "carrier": "USPS", "tracking": "9400 1118 9922 3456 7890 12"},
    "TM-47790": {"item": "rainbow-romper", "buyer": "TutuTessa", "seller": "RainbowRosa", "status": "Completed", "date": "Tue 13 Oct", "tax": 2.57, "total": 31.57},
    "TM-48140": {"item": "rainbow-sweater", "buyer": "TutuTessa", "seller": "HandMeDownHana", "status": "Delivered", "date": "Mon 19 Oct", "tax": 3.28, "total": 40.28, "delivered": "Sat 24 Oct"},
    # Tessa's sales
    "TM-48302": {"item": "heart-hoodie", "buyer": "PuddleJumperPia", "seller": "TutuTessa", "status": "Paid", "date": "Sat 24 Oct", "tax": 3.15, "total": 48.15, "ship_by": "Fri 30 Oct",
                  "address": ["Pia Reyes", "1450 Larimer St, Apt 6B", "Denver, CO 80202"]},
    "TM-47511": {"item": "floral-shorts", "buyer": "PuddleJumperPia", "seller": "TutuTessa", "status": "Completed", "date": "Fri 2 Oct", "tax": 2.59, "total": 39.59, "payout": 33.30, "fee": 3.70},
}

SAVED_ADDRESS = ["Tessa Bell", "214 Kent Ave, Apt 3R", "Brooklyn, NY 11249"]
CARD = ("Visa", "4242")
