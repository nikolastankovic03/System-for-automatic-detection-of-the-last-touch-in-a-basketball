from datetime import datetime
import sqlite3
from flask import Flask, render_template_string, jsonify, request

app = Flask(__name__)
DB_NAME = "diplomski_lopta.db"

def init_db():
    conn = sqlite3.connect(DB_NAME)
    c = conn.cursor()
    c.execute("""
        CREATE TABLE IF NOT EXISTS dodiri (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            timestamp TEXT NOT NULL,
            igrac TEXT,
            magnituda INTEGER
        )
    """)
    
    # Automatski dodaje kolonu 'udaljenost' ako ne postoji, čuvajući postojeću bazu
    try:
        c.execute("ALTER TABLE dodiri ADD COLUMN udaljenost REAL;")
        conn.commit()
    except sqlite3.OperationalError:
        pass # Kolona već postoji, sve je u redu
        
    conn.close()

# --- WEB DASHBOARD (PRIKAZ U REALNOM VREMENU) ---
HTML_STRANICA = """
<!DOCTYPE html>
<html lang="sr">
<head>
    <meta charset="UTF-8">
    <title>Prikaz Udaraca Uživo</title>
    <style>
        body { font-family: Arial, sans-serif; margin: 30px; background-color: #f4f4f9; }
        h2 { color: #333; }
        table { width: 100%; border-collapse: collapse; margin-top: 20px; background: white; }
        th, td { border: 1px solid #ddd; padding: 12px; text-align: left; }
        th { background-color: #007bff; color: white; }
        tr:nth-child(even) { background-color: #f9f9f9; }
        .live-indicator { display: inline-block; width: 10px; height: 10px; background-color: #28a745; border-radius: 50%; margin-right: 5px; }
    </style>
</head>
<body>
    <h2><span class="live-indicator"></span> Istorija Udaraca (Uživo Prikaz)</h2>
    <p>Tabela se automatski osvežava svake sekunde.</p>

    <table>
        <thead>
            <tr>
                <th>Broj udarca</th>
                <th>Vreme udarca</th>
                <th>Igrač</th>
                <th>Magnituda</th>
                <th>Udaljenost (m)</th>
            </tr>
        </thead>
        <tbody id="tabela-body">
            <!-- Podaci stižu ovde automatski -->
        </tbody>
    </table>

    <script>
        function osveziTabelu() {
            fetch('/istorija')
                .then(response => response.json())
                .then(data => {
                    const tbody = document.getElementById('tabela-body');
                    tbody.innerHTML = ''; 

                    data.forEach(red => {
                        const tr = document.createElement('tr');
                        tr.innerHTML = `
                            <td>${red.id}</td>
                            <td>${red.timestamp}</td>
                            <td><b>${red.igrac}</b></td>
                            <td>${red.magnituda}</td>
                            <td>${red.udaljenost !== undefined ? red.udaljenost : 0.0} m</td>
                        `;
                        tbody.appendChild(tr);
                    });
                })
                .catch(err => console.error("Greška pri dohvatanju podataka:", err));
        }

        setInterval(osveziTabelu, 1000);
        osveziTabelu();
    </script>
</body>
</html>
"""

@app.route("/")
def index():
    return render_template_string(HTML_STRANICA)

@app.route("/dodir", methods=["POST"])
def zabelezi_dodir():
    data = request.get_json(silent=True) or request.form or {}

    magnituda = data.get("magnituda", 0)
    igrac = data.get("igrac", "Nepoznat")
    udaljenost = data.get("udaljenost", 0.0)
    timestamp = datetime.now().strftime("%Y-%m-%d %H:%M:%S.%f")[:-3]

    conn = sqlite3.connect(DB_NAME)
    c = conn.cursor()
    c.execute(
        "INSERT INTO dodiri (timestamp, igrac, magnituda, udaljenost) VALUES (?, ?, ?, ?)",
        (timestamp, igrac, magnituda, udaljenost),
    )
    conn.commit()
    conn.close()

    print(f"[{timestamp}] UDARAC! Igrac: {igrac} | Magnituda: {magnituda} | Udaljenost: {udaljenost}m")

    return jsonify({"status": "ok", "timestamp": timestamp}), 200

@app.route("/istorija", methods=["GET"])
def prikazi_istoriju():
    conn = sqlite3.connect(DB_NAME)
    c = conn.cursor()
    c.execute("SELECT id, timestamp, igrac, magnituda, udaljenost FROM dodiri ORDER BY id DESC LIMIT 20")
    rows = c.fetchall()
    conn.close()

    rezultat = [
        {
            "id": r[0],
            "timestamp": r[1],
            "igrac": r[2],
            "magnituda": r[3],
            "udaljenost": r[4] if r[4] is not None else 0.0
        }
        for r in rows
    ]
    return jsonify(rezultat), 200

if __name__ == "__main__":
    init_db()
    print("Server pokrenut. Otvori web browser na: http://127.0.0.1:5000")
    app.run(host="0.0.0.0", port=5000, debug=True)