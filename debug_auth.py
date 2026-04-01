import httpx, base64, json

# Get token
r = httpx.post(
    "https://localhost:44399/api/auth/dev-admin-token", verify=False, timeout=10
)
jwt = r.json()["token"]
print(f"Token: {jwt[:50]}...")

# Check claims as seen by the server
r2 = httpx.get(
    "https://localhost:44399/api/auth/dev-claims",
    headers={"Authorization": f"Bearer {jwt}"},
    verify=False,
    timeout=10,
)
print(f"\nServer-side claims ({r2.status_code}):")
for claim in r2.json():
    print(f"  {claim['type']} = {claim['value']}")

# Test admin endpoint
r3 = httpx.get(
    "https://localhost:44399/api/admin/pending-changes?pageSize=1",
    headers={"Authorization": f"Bearer {jwt}"},
    verify=False,
    timeout=10,
)
print(f"\n/admin/pending-changes: {r3.status_code}")
