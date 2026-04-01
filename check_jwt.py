import httpx, base64, json

# Get token
r = httpx.post(
    "https://localhost:44399/api/auth/dev-admin-token", verify=False, timeout=10
)
jwt = r.json()["token"]
print(f"Status: {r.status_code}")

# Decode payload (no verify)
parts = jwt.split(".")
payload = parts[1] + "=" * (4 - len(parts[1]) % 4)
decoded = json.loads(base64.b64decode(payload))
print(f"Claims: {json.dumps(decoded, indent=2)}")

# Test the endpoint with the token
r2 = httpx.get(
    "https://localhost:44399/api/admin/pending-changes?pageSize=1",
    headers={"Authorization": f"Bearer {jwt}"},
    verify=False,
    timeout=10,
)
print(f"Admin endpoint status: {r2.status_code}")
print(f"Response: {r2.text[:200]}")
