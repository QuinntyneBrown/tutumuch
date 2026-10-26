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

# Every item is girls' clothing; the one accepted size is Girls' in every category.
# tone = photo backdrop 1..6, img = assets/items/<img>.svg
ITEMS = {
    "pink-tutu": {"id": "L-20931", "title": "Pink tulle tutu skirt", "brand": "Janie and Jack", "cat": "Dresses", "sub": "Tutus", "cond": "Used - Excellent", "price": 32, "ship": 6, "seller": "TwirlyTara", "img": "tutu", "tone": 6, "listed": "2 hours ago", "photos": 5},
    "party-dress": {"id": "L-20877", "title": "Floral party dress", "brand": "Mini Boden", "cat": "Dresses", "sub": "Dresses", "cond": "Used - Good", "price": 42, "ship": 7, "seller": "HandMeDownHana", "img": "party-dress", "tone": 1, "listed": "Yesterday", "photos": 6},
    "rainbow-romper": {"id": "L-20902", "title": "Rainbow stripe romper", "brand": "Tea Collection", "cat": "Dresses", "sub": "Rompers", "cond": "New with tags", "price": 24, "ship": 5, "seller": "RainbowRosa", "img": "romper", "tone": 2, "listed": "5 hours ago", "photos": 4},
    "velvet-dress": {"id": "L-20915", "title": "Velvet holiday dress", "brand": "Janie and Jack", "cat": "Dresses", "sub": "Dresses", "cond": "New with tags", "price": 58, "ship": 0, "seller": "TwirlyTara", "img": "holiday-dress", "tone": 4, "listed": "3 hours ago", "photos": 5},
    "pleated-skirt": {"id": "L-20811", "title": "Pleated plaid skirt", "brand": "Zara Kids", "cat": "Dresses", "sub": "Skirts", "cond": "Used - Excellent", "price": 26, "ship": 5, "seller": "TwirlyTara", "img": "skirt", "tone": 3, "listed": "3 days ago", "photos": 6},
    "sundress": {"id": "L-20790", "title": "Yellow gingham sundress", "brand": "Carter's", "cat": "Dresses", "sub": "Dresses", "cond": "Used - Fair", "price": 12, "ship": 5, "seller": "TutuTessa", "img": "sundress", "tone": 5, "listed": "4 days ago", "photos": 3, "flaws": "Small mark on the hem. One shoulder tie re-stitched."},
    "rainbow-sweater": {"id": "L-20866", "title": "Rainbow knit sweater", "brand": "Mini Boden", "cat": "Tops", "sub": "Sweaters", "cond": "Used - Excellent", "price": 32, "ship": 5, "seller": "HandMeDownHana", "img": "sweater", "tone": 1, "listed": "Yesterday", "photos": 4},
    "heart-hoodie": {"id": "L-20855", "title": "Pink heart hoodie", "brand": "Gap Kids", "cat": "Tops", "sub": "Hoodies", "cond": "Used - Good", "price": 38, "ship": 7, "seller": "TutuTessa", "img": "hoodie", "tone": 6, "listed": "2 days ago", "photos": 3},
    "collar-blouse": {"id": "L-20921", "title": "Peter Pan collar blouse", "brand": "Zara Kids", "cat": "Tops", "sub": "Shirts", "cond": "New with tags", "price": 18, "ship": 5, "seller": "RainbowRosa", "img": "blouse", "tone": 2, "listed": "4 hours ago", "photos": 2},
    "unicorn-tee": {"id": "L-20899", "title": "Unicorn graphic T-shirt", "brand": "H&M Kids", "cat": "Tops", "sub": "T-shirts", "cond": "New without tags", "price": 12, "ship": 5, "seller": "RainbowRosa", "img": "tee", "tone": 3, "listed": "6 hours ago", "photos": 2},
    "puffer-jacket": {"id": "L-20840", "title": "Purple puffer jacket", "brand": "Old Navy", "cat": "Outerwear", "sub": "Jackets", "cond": "Used - Good", "price": 34, "ship": 8, "seller": "HandMeDownHana", "img": "puffer", "tone": 5, "listed": "2 days ago", "photos": 6},
    "raincoat": {"id": "L-20802", "title": "Yellow raincoat", "brand": "Hanna Andersson", "cat": "Outerwear", "sub": "Coats", "cond": "Used - Good", "price": 44, "ship": 8, "seller": "HandMeDownHana", "img": "raincoat", "tone": 2, "listed": "4 days ago", "photos": 7, "status": "Reserved"},
    "denim-leggings": {"id": "L-20870", "title": "Denim leggings", "brand": "OshKosh B'gosh", "cat": "Bottoms", "sub": "Leggings", "cond": "New without tags", "price": 16, "ship": 5, "seller": "TwirlyTara", "img": "leggings", "tone": 4, "listed": "Yesterday", "photos": 4},
    "floral-shorts": {"id": "L-20760", "title": "Floral cotton shorts", "brand": "Carter's", "cat": "Bottoms", "sub": "Shorts", "cond": "Used - Excellent", "price": 30, "ship": 7, "seller": "TutuTessa", "img": "shorts", "tone": 6, "listed": "6 days ago", "photos": 3},
}

# Catalog order, newest first (L2-022).
CATALOG = ["pink-tutu", "velvet-dress", "collar-blouse", "rainbow-romper", "unicorn-tee", "party-dress", "rainbow-sweater", "denim-leggings",
           "heart-hoodie", "puffer-jacket", "pleated-skirt", "raincoat", "sundress", "floral-shorts"]

# (category, subcategories, the one accepted size). The size is stored as "Girls'"
# and shown on size tags as "Girls".
CATEGORIES = [
    ("Dresses", ["Dresses", "Skirts", "Tutus", "Rompers"], "Girls'"),
    ("Tops", ["Shirts", "T-shirts", "Sweaters", "Hoodies"], "Girls'"),
    ("Outerwear", ["Jackets", "Coats"], "Girls'"),
    ("Bottoms", ["Pants", "Leggings", "Shorts"], "Girls'"),
]
CONDITIONS = ["New with tags", "New without tags", "Used - Excellent", "Used - Good", "Used - Fair"]
BRANDS = ["Carter's", "OshKosh B'gosh", "Gap Kids", "Old Navy", "Hanna Andersson", "Mini Boden", "Janie and Jack", "Tea Collection", "Zara Kids", "H&M Kids"]

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
