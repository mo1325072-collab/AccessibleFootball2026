import urllib.request
import re

url = 'https://www.transfermarkt.com/zamalek-sc/kader/verein/664'
req = urllib.request.Request(url, headers={'User-Agent': 'Mozilla/5.0 (Windows NT 10.0; Win64; x64)'})
try:
    with urllib.request.urlopen(req, timeout=10) as resp:
        html = resp.read().decode('utf-8', errors='ignore')
        matches = re.findall(r'<a\s+title="([^"]+)"\s+href="[^"]*profil/spieler/\d+"', html)
        if not matches:
            matches = re.findall(r'href="[^"]*profil/spieler/\d+"[^>]*>([^<]+)</a>', html)
        seen = set()
        squad = []
        for m in matches:
            m = m.strip()
            if m and m not in seen and len(m) > 2:
                seen.add(m)
                squad.append(m)
        print(f"Total players found: {len(squad)}")
        for i, p in enumerate(squad):
            print(f"{i+1:2d}. {p}")
except Exception as e:
    print("Error:", e)
