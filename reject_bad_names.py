import httpx
import subprocess

# Get JWT
r = httpx.post(
    "https://localhost:44399/api/auth/dev-admin-token", verify=False, timeout=10
)
jwt = r.json()["token"]
headers = {"Authorization": f"Bearer {jwt}"}

# Get pending IDs
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
)
ids = [
    line.strip()
    for line in result.stdout.decode("utf-8", errors="replace").strip().split("\n")
    if line.strip()
]
print(f"Found {len(ids)} bad-name products to reject")

rejected = 0
for change_id in ids:
    r = httpx.post(
        f"https://localhost:44399/api/admin/pending-changes/{change_id}/reject",
        headers=headers,
        json={"note": "Nombre invalido — slug vacio despues de limpiar emojis"},
        verify=False,
        timeout=15,
    )
    if r.status_code == 200:
        print(f"  Rejected: {change_id[:8]}...")
        rejected += 1
    else:
        print(f"  FAIL {r.status_code}: {change_id[:8]}... -> {r.text[:80]}")

print(f"\nDone: {rejected} rejected")
