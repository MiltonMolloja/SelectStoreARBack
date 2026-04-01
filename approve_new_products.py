import httpx
import subprocess

# 1. Obtener JWT de admin
r = httpx.post(
    "https://localhost:44399/api/auth/dev-admin-token", verify=False, timeout=10
)
if r.status_code != 200:
    print(f"ERROR getting token: {r.status_code} {r.text}")
    exit(1)

jwt = r.json()["token"]
print(f"JWT obtained: {jwt[:40]}...")

headers = {"Authorization": f"Bearer {jwt}"}

# 2. Obtener IDs de cambios Pending (Created)
result = subprocess.run(
    [
        "docker",
        "exec",
        "selectstorear-db-1",
        "psql",
        "-U",
        "postgres",
        "-d",
        "selectstorear",
        "-t",
        "-A",
        "-c",
        'SELECT "Id" FROM product_pending_changes WHERE change_type = \'Created\' AND "Status" = \'Pending\' AND "ProductId" IS NULL',
    ],
    capture_output=True,
    text=True,
)
ids = [line.strip() for line in result.stdout.strip().split("\n") if line.strip()]
print(f"Found {len(ids)} new products to create")

# 3. Aprobar cada uno
approved = 0
errors = 0
for change_id in ids:
    try:
        r = httpx.post(
            f"https://localhost:44399/api/admin/pending-changes/{change_id}/approve",
            headers=headers,
            verify=False,
            timeout=30,
        )
        if r.status_code == 200:
            data = r.json()
            print(f"  OK: {change_id[:8]}... -> {data.get('message', 'approved')}")
            approved += 1
        else:
            print(f"  FAIL {r.status_code}: {change_id[:8]}... -> {r.text[:100]}")
            errors += 1
    except Exception as e:
        print(f"  ERROR: {change_id[:8]}... -> {e}")
        errors += 1

print(f"\nDone: {approved} created, {errors} errors")
