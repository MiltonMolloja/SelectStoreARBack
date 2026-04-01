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
        "-c",
        'SELECT change_type, "Status", COUNT(*) FROM product_pending_changes GROUP BY change_type, "Status" ORDER BY change_type, "Status"',
    ],
    capture_output=True,
)
print(result.stdout.decode("utf-8", errors="replace"))

# Products count
result2 = subprocess.run(
    [
        "docker",
        "exec",
        "selectstorear-db-1",
        "psql",
        "-U",
        "postgres",
        "-d",
        "selectstorear",
        "-c",
        "SELECT COUNT(*) as total_products FROM products",
    ],
    capture_output=True,
)
print(result2.stdout.decode("utf-8", errors="replace"))
