"""Cast and catalog shared by every mock. Today is Sun 25 Oct 2026.

Reuse these people, items and orders everywhere so a reviewer can follow one
story across screens. Never invent a new person or item in a screen module;
add it here first.
"""

TODAY = "Sun 25 Oct 2026"
FEE_PERCENT = 10

# Display names are public. Real names appear only to the member themself
# and on a seller's shipping label.
PEOPLE = {
    "marcus_12": {"name": "Marcus Bell", "initials": "MB", "city": "Brooklyn, NY", "since": "Mar 2025", "rating": 4.8, "reviews": 23, "sales": 19, "email": "marcus.bell@example.com", "bio": "Size 12 since high school. Mostly sneakers, the odd jacket."},
    "kicks_12": {"name": "Dre Okafor", "initials": "DO", "city": "Atlanta, GA", "since": "Jan 2025", "rating": 4.9, "reviews": 128, "sales": 141, "bio": "Clean pairs, real photos, ships next day."},
    "vintage_lou": {"name": "Lou Ferreira", "initials": "LF", "city": "Portland, OR", "since": "Jun 2025", "rating": 4.7, "reviews": 41, "sales": 46, "bio": "Workwear and waxed cotton. Everything measured flat."},
    "tall_tom": {"name": "Tom Reyes", "initials": "TR", "city": "Denver, CO", "since": "Aug 2026", "rating": 5.0, "reviews": 2, "sales": 0, "bio": ""},
    "jaybird": {"name": "Jay Park", "initials": "JP", "city": "Chicago, IL", "since": "Oct 2026", "rating": None, "reviews": 0, "sales": 0, "bio": "Clearing out the closet."},
}
ME = "marcus_12"          # the signed-in member in member mocks
ADMIN = {"name": "Ana Ruiz", "initials": "AR", "handle": "ana.admin"}

# Every item is men's US 12 (Shoes) or men's L (everything else).
# tone = photo backdrop 1..6, img = assets/items/<img>.svg
ITEMS = {
    "air-max-90": {"id": "L-20931", "title": "Air Max 90, white", "brand": "Nike", "cat": "Shoes", "sub": "Sneakers", "cond": "Used - Excellent", "price": 85, "ship": 9, "seller": "kicks_12", "img": "sneaker-white", "tone": 6, "listed": "2 hours ago", "photos": 5},
    "iron-ranger": {"id": "L-20877", "title": "Iron Ranger 8111 boots", "brand": "Red Wing", "cat": "Shoes", "sub": "Boots", "cond": "Used - Good", "price": 210, "ship": 15, "seller": "vintage_lou", "img": "boot", "tone": 1, "listed": "Yesterday", "photos": 6},
    "samba-og": {"id": "L-20902", "title": "Samba OG", "brand": "Adidas", "cat": "Shoes", "sub": "Sneakers", "cond": "New with tags", "price": 70, "ship": 8, "seller": "jaybird", "img": "sneaker-samba", "tone": 2, "listed": "5 hours ago", "photos": 4},
    "nb-990": {"id": "L-20915", "title": "990v6, grey", "brand": "New Balance", "cat": "Shoes", "sub": "Sneakers", "cond": "New with tags", "price": 150, "ship": 0, "seller": "kicks_12", "img": "sneaker-grey", "tone": 4, "listed": "3 hours ago", "photos": 5},
    "park-avenue": {"id": "L-20811", "title": "Park Avenue oxfords", "brand": "Allen Edmonds", "cat": "Shoes", "sub": "Dress shoes", "cond": "Used - Excellent", "price": 140, "ship": 12, "seller": "kicks_12", "img": "oxford", "tone": 3, "listed": "3 days ago", "photos": 6},
    "arizona": {"id": "L-20790", "title": "Arizona sandals", "brand": "Birkenstock", "cat": "Shoes", "sub": "Sandals", "cond": "Used - Fair", "price": 45, "ship": 8, "seller": "marcus_12", "img": "sandal", "tone": 5, "listed": "4 days ago", "photos": 3, "flaws": "Footbed worn at the heel. Small scuff on the left strap."},
    "better-sweater": {"id": "L-20866", "title": "Better Sweater fleece", "brand": "Patagonia", "cat": "Tops", "sub": "Sweaters", "cond": "Used - Excellent", "price": 48, "ship": 7, "seller": "vintage_lou", "img": "sweater", "tone": 1, "listed": "Yesterday", "photos": 4},
    "reverse-weave": {"id": "L-20855", "title": "Reverse Weave hoodie, grey", "brand": "Champion", "cat": "Tops", "sub": "Hoodies", "cond": "Used - Good", "price": 38, "ship": 7, "seller": "marcus_12", "img": "hoodie", "tone": 6, "listed": "2 days ago", "photos": 3},
    "oxford-shirt": {"id": "L-20921", "title": "Oxford shirt, blue", "brand": "Uniqlo", "cat": "Tops", "sub": "Shirts", "cond": "New with tags", "price": 18, "ship": 5, "seller": "jaybird", "img": "shirt", "tone": 2, "listed": "4 hours ago", "photos": 2},
    "organic-tee": {"id": "L-20899", "title": "Organic cotton tee, bone", "brand": "Everlane", "cat": "Tops", "sub": "T-shirts", "cond": "New without tags", "price": 22, "ship": 5, "seller": "jaybird", "img": "tee", "tone": 3, "listed": "6 hours ago", "photos": 2},
    "detroit-jacket": {"id": "L-20840", "title": "Detroit jacket, brown duck", "brand": "Carhartt", "cat": "Outerwear", "sub": "Jackets", "cond": "Used - Good", "price": 95, "ship": 12, "seller": "vintage_lou", "img": "jacket", "tone": 5, "listed": "2 days ago", "photos": 6},
    "bedale": {"id": "L-20802", "title": "Bedale waxed jacket", "brand": "Barbour", "cat": "Outerwear", "sub": "Coats", "cond": "Used - Good", "price": 165, "ship": 14, "seller": "vintage_lou", "img": "coat", "tone": 2, "listed": "4 days ago", "photos": 7, "status": "Reserved"},
    "gamma-pants": {"id": "L-20870", "title": "Gamma LT pants", "brand": "Arc'teryx", "cat": "Bottoms", "sub": "Pants", "cond": "New without tags", "price": 110, "ship": 9, "seller": "kicks_12", "img": "pants", "tone": 4, "listed": "Yesterday", "photos": 4},
    "tech-shorts": {"id": "L-20760", "title": "Tech Fleece shorts", "brand": "Nike", "cat": "Bottoms", "sub": "Shorts", "cond": "Used - Excellent", "price": 30, "ship": 7, "seller": "marcus_12", "img": "shorts", "tone": 6, "listed": "6 days ago", "photos": 3},
}

# Catalog order, newest first (L2-022).
CATALOG = ["air-max-90", "nb-990", "oxford-shirt", "samba-og", "organic-tee", "iron-ranger", "better-sweater", "gamma-pants",
           "reverse-weave", "detroit-jacket", "park-avenue", "bedale", "arizona", "tech-shorts"]

CATEGORIES = [
    ("Shoes", ["Sneakers", "Boots", "Dress shoes", "Sandals"], "US 12"),
    ("Tops", ["Shirts", "T-shirts", "Sweaters", "Hoodies"], "L"),
    ("Outerwear", ["Jackets", "Coats"], "L"),
    ("Bottoms", ["Pants", "Shorts"], "L"),
]
CONDITIONS = ["New with tags", "New without tags", "Used - Excellent", "Used - Good", "Used - Fair"]
BRANDS = ["Nike", "Adidas", "New Balance", "Red Wing", "Allen Edmonds", "Birkenstock", "Patagonia", "Champion", "Carhartt", "Barbour", "Arc'teryx", "Uniqlo", "Everlane"]

# Orders. Totals: price + shipping + tax. Fee is 10% of item + shipping (L2-031).
ORDERS = {
    # Marcus's purchases
    "TM-48213": {"item": "iron-ranger", "buyer": "marcus_12", "seller": "vintage_lou", "status": "Shipped", "date": "Thu 22 Oct", "tax": 19.97, "total": 244.97, "carrier": "USPS", "tracking": "9400 1118 9922 3456 7890 12"},
    "TM-47790": {"item": "samba-og", "buyer": "marcus_12", "seller": "jaybird", "status": "Completed", "date": "Tue 13 Oct", "tax": 6.92, "total": 84.92},
    "TM-48140": {"item": "better-sweater", "buyer": "marcus_12", "seller": "vintage_lou", "status": "Delivered", "date": "Mon 19 Oct", "tax": 4.88, "total": 59.88, "delivered": "Sat 24 Oct"},
    # Marcus's sales
    "TM-48302": {"item": "reverse-weave", "buyer": "tall_tom", "seller": "marcus_12", "status": "Paid", "date": "Sat 24 Oct", "tax": 3.15, "total": 48.15, "ship_by": "Fri 30 Oct",
                  "address": ["Tom Reyes", "1450 Larimer St, Apt 6B", "Denver, CO 80202"]},
    "TM-47511": {"item": "tech-shorts", "buyer": "tall_tom", "seller": "marcus_12", "status": "Completed", "date": "Fri 2 Oct", "tax": 2.59, "total": 39.59, "payout": 33.30, "fee": 3.70},
}

SAVED_ADDRESS = ["Marcus Bell", "214 Kent Ave, Apt 3R", "Brooklyn, NY 11249"]
CARD = ("Visa", "4242")
