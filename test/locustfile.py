import time
from locust import HttpUser, task, between
from faker import Faker

fake = Faker()
Faker.seed(0)

class ShoppingCartUser(HttpUser):
    wait_time = between(1, 2.5)

    @task(8)
    def get(self):
        id = fake.random_int(1, 10000)
        self.client.get(f"/shopping_cart/{id}", name='/shopping_cart/{id}')

    @task(2)
    def put(self):
        user_id = fake.random_int(1, 1000000) 
        payload = {
            "user_id": user_id,
            "items": [                
            ]
        }
        for _ in range(fake.random_int(1,10)):
            item_id = fake.random_int(1, 10000) 
            quantity = fake.random_int(1, 10) 
            price = fake.random_int(1, 100000) / 100.0
            item = {
                    "id": item_id,
                    "quantity": quantity,
                    "price": price,
                    "details": dict()           
                }
            for _ in range(fake.random_int(1,10)):
                key = fake.lexify("?" * fake.random_int(3,7))
                value = fake.lexify("?" * fake.random_int(3,100))
                item["details"][key] = value
            payload["items"].append(item)

        self.client.put("/shopping_cart", json=payload)
