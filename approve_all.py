import subprocess
import httpx

# Obtener IDs pendientes via psql
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
        'SELECT "Id" FROM product_pending_changes WHERE "Status" = \'Pending\'',
    ],
    capture_output=True,
    text=True,
)
ids = [line.strip() for line in result.stdout.strip().split("\n") if line.strip()]
print(f"Found {len(ids)} pending changes")

approved = 0
errors = 0
for change_id in ids:
    try:
        r = httpx.post(
            f"https://localhost:44399/api/admin/pending-changes/{change_id}/approve",
            headers={
                "Authorization": "Bearer dev-telegram-sync-key-change-in-production"
            },
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

print(f"\nDone: {approved} approved, {errors} errors")
