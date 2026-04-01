import subprocess

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
        "SELECT proposed_name, proposed_brand FROM product_pending_changes WHERE change_type = 'Created' AND \"Status\" = 'Pending'",
    ],
    capture_output=True,
    text=True,
)
print("Remaining pending products with bad names:")
for line in result.stdout.strip().split("\n"):
    if line.strip():
        print(f"  {line}")
print(f"\nTotal: {len([l for l in result.stdout.strip().split(chr(10)) if l.strip()])}")
