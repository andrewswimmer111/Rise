"""Research-only point-placement comparison. No Unity dependencies or game edits.
Run: python3 docs/spacing-experiment.py
Outputs deterministic SVG and JSON beside this script. Units are arbitrary.
"""
import json
import math
import random
import statistics
from pathlib import Path

OUT = Path(__file__).parent
W, H, N = 6, 24, 24
METHODS = ['Independent', 'Jittered cells', 'Minimum distance', 'Best of 16']

def distance(a, b):
    return math.hypot(a[0]-b[0], a[1]-b[1])

def generate(method, seed):
    rng = random.Random(seed)
    point = lambda: (rng.random()*W, rng.random()*H)
    points = []
    if method == 'Jittered cells':
        return [((x+rng.random())*2, (y+rng.random())*3)
                for y in range(8) for x in range(3)]
    for _ in range(N):
        if method == 'Independent':
            points.append(point())
        elif method == 'Minimum distance':
            for attempt in range(10000):
                candidate = point()
                if all(distance(candidate, p) >= 1.25 for p in points):
                    points.append(candidate)
                    break
            else:
                raise RuntimeError('Could not fit requested count')
        else:
            candidates = [point() for _ in range(16)]
            points.append(max(candidates, key=lambda q:
                              min((distance(q,p) for p in points), default=999)))
    return points

def metrics(points):
    nearest = [min(distance(p,q) for j,q in enumerate(points) if i != j)
               for i,p in enumerate(points)]
    close = sum(distance(p,q) < .8 for i,p in enumerate(points) for q in points[i+1:])
    probes = [((x+.5)*W/12, (y+.5)*H/48) for x in range(12) for y in range(48)]
    # This is sampled covering distance, not an exact empty-circle calculation.
    hole = max(min(distance(q,p) for p in points) for q in probes)
    return {'close_pairs_under_0.8':close, 'mean_nearest_distance':statistics.mean(nearest),
            'nearest_distance_cv':statistics.pstdev(nearest)/statistics.mean(nearest),
            'sampled_max_empty_distance':hole}

results = {}
for method in METHODS:
    runs = [metrics(generate(method, seed)) for seed in range(100)]
    results[method] = {key:round(statistics.mean(r[key] for r in runs),4) for key in runs[0]}
payload = {'seeds':'0..99', 'domain':[W,H], 'points':N, 'results':results,
           'limitations':'Static points only; no physics, shields, routes, saw footprints or perception study. Minimum distance is rejection sampling, not Bridson. Best-of-16 has no hard distance guarantee.'}
(OUT/'spacing-experiment-results.json').write_text(json.dumps(payload, indent=2)+'\n')
svg = ['<svg xmlns="http://www.w3.org/2000/svg" width="1000" height="710" viewBox="0 0 1000 710">',
       '<rect width="1000" height="710" fill="#f5f7fa"/>',
       '<g font-family="Arial,sans-serif" fill="#182c40">',
       '<text x="30" y="35" font-size="23" font-weight="bold">Random placement without heavy clusters</text>',
       '<text x="30" y="60" font-size="14">24 points per panel. Seed 0, chosen before comparison. Illustrations, not game layouts.</text>']
for i,method in enumerate(METHODS):
    x0 = 30+i*245
    svg += [f'<text x="{x0}" y="98" font-size="18" font-weight="bold">{method}</text>',
            f'<rect x="{x0}" y="115" width="120" height="480" fill="white" stroke="#aab8c5"/>']
    for x,y in generate(method,0):
        svg.append(f'<circle cx="{x0+x*20:.2f}" cy="{115+y*20:.2f}" r="5" fill="#285f89"/>')
    r=results[method]
    svg += [f'<text x="{x0}" y="625" font-size="13">Close pairs: {r["close_pairs_under_0.8"]:.2f}</text>',
            f'<text x="{x0}" y="645" font-size="13">Empty distance: {r["sampled_max_empty_distance"]:.2f}</text>']
svg += ['<text x="30" y="686" font-size="13">Metrics average 100 seeds. Close = under 0.8 units; empty distance uses 576 probes. Lower is not always more fun.</text>', '</g></svg>']
(OUT/'spacing-comparison.svg').write_text('\n'.join(svg)+'\n')
print(json.dumps(payload,indent=2))
