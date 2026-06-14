#!/usr/bin/env python3
"""scene_inspect.py - compact, READ-ONLY dump of a Unity .unity / .prefab.

Resolves the verbose fileID-linked YAML into a readable GameObject tree:
hierarchy + active state + transform + components (MonoBehaviours resolved to
class names). Pure stdlib, no Unity, no deps. NEVER writes/modifies anything.

Usage:
  python Claude/tools/scene_inspect.py <scene_or_prefab> [options]

Options:
  -f, --fields          Dump each MonoBehaviour's serialized user fields
  -o, --object SUBSTR   Only show subtrees whose GameObject name contains SUBSTR
  -d, --depth N         Limit hierarchy depth (token control)
      --settings        Also list scene-level settings docs (Render/Lightmap/...)
      --unicode         Use unicode markers (default ASCII, safe on any console)

Markers: + active   - inactive   ~ prefab instance
"""
import sys, os, re, argparse

# Output is UTF-8 regardless of console codepage (handles Cyrillic/non-ASCII names).
try:
    sys.stdout.reconfigure(encoding="utf-8", errors="replace")
except Exception:
    pass

# --- Unity classID -> friendly name (common components; unknowns show u!<id>) ---
CLASS_NAMES = {
    1: "GameObject", 4: "Transform", 224: "RectTransform", 222: "CanvasRenderer",
    20: "Camera", 23: "MeshRenderer", 33: "MeshFilter", 137: "SkinnedMeshRenderer",
    54: "Rigidbody", 50: "Rigidbody2D",
    64: "MeshCollider", 65: "BoxCollider", 135: "SphereCollider",
    136: "CapsuleCollider", 154: "TerrainCollider",
    58: "CircleCollider2D", 60: "PolygonCollider2D", 61: "BoxCollider2D", 68: "EdgeCollider2D",
    95: "Animator", 111: "Animation",
    81: "AudioListener", 82: "AudioSource",
    108: "Light", 220: "LightProbeGroup", 215: "ReflectionProbe",
    198: "ParticleSystem", 199: "ParticleSystemRenderer", 96: "TrailRenderer", 120: "LineRenderer",
    212: "SpriteRenderer", 114: "MonoBehaviour", 115: "MonoScript",
    223: "Canvas", 225: "CanvasGroup",
    195: "NavMeshAgent", 208: "NavMeshObstacle", 156: "Grid",
    1001: "PrefabInstance",
}
# settings / non-GameObject docs we skip in the tree
SETTINGS_CLASSES = {29, 104, 157, 196, 1660057539, 850595691}  # Occlusion/Render/Lightmap/NavMesh/SceneRoots/LightingSettings

FLOAT = r"-?\d+(?:\.\d+)?(?:[eE]-?\d+)?"
DOC_RE = re.compile(r"^--- !u!(\d+) &(-?\d+)(?:\s+(stripped))?\s*$")
TYPE_RE = re.compile(r"^([A-Za-z_][A-Za-z0-9_]*):\s*$")


def fmt(v):
    """Trim a float string: 1.0 -> 1, 0.30000001 -> 0.3, -0 -> 0."""
    try:
        f = round(float(v), 4)
    except ValueError:
        return v
    if f == 0:
        f = 0.0
    s = ("%f" % f).rstrip("0").rstrip(".")
    return s if s else "0"


def vec(body, key, n=3):
    m = re.search(r"(?m)^\s*" + re.escape(key) + r":\s*\{x:\s*(" + FLOAT + r"),\s*y:\s*(" + FLOAT +
                  r"),\s*z:\s*(" + FLOAT + r")" + (r",\s*w:\s*(" + FLOAT + r")" if n == 4 else "") + r"\}", body)
    if not m:
        return None
    return tuple(fmt(m.group(i + 1)) for i in range(n))


def scalar(body, key):
    m = re.search(r"(?m)^\s*" + re.escape(key) + r":\s*(.*?)\s*$", body)
    return m.group(1) if m else None


def first_fileid(body, key):
    m = re.search(r"(?m)^\s*" + re.escape(key) + r":\s*\{fileID:\s*(-?\d+)", body)
    return int(m.group(1)) if m else None


def parse_docs(path):
    docs, cur = [], None
    with open(path, encoding="utf-8", errors="replace") as f:
        for line in f:
            line = line.rstrip("\n")
            m = DOC_RE.match(line)
            if m:
                if cur:
                    docs.append(cur)
                cur = {"cls": int(m.group(1)), "id": int(m.group(2)),
                       "stripped": bool(m.group(3)), "type": None, "body": []}
                continue
            if cur is None:
                continue
            if cur["type"] is None:
                tm = TYPE_RE.match(line)
                if tm:
                    cur["type"] = tm.group(1)
                    continue
            cur["body"].append(line)
    if cur:
        docs.append(cur)
    for d in docs:
        d["text"] = "\n".join(d["body"])
    return docs


def build_guid_map(needed, roots):
    """guid -> asset filename (e.g. 'Foo.cs', 'Bar.prefab'). Only resolves needed guids."""
    out, needed = {}, set(needed)
    guid_re = re.compile(r"^guid:\s*([0-9a-fA-F]{32})")
    for root in roots:
        if not os.path.isdir(root):
            continue
        for dp, _dirs, files in os.walk(root):
            for fn in files:
                if not fn.endswith(".meta"):
                    continue
                try:
                    with open(os.path.join(dp, fn), encoding="utf-8", errors="replace") as fh:
                        for _ in range(8):
                            ln = fh.readline()
                            gm = guid_re.match(ln)
                            if gm:
                                g = gm.group(1).lower()
                                if g in needed and g not in out:
                                    out[g] = fn[:-5]  # strip '.meta'
                                break
                except OSError:
                    pass
            if len(out) >= len(needed):
                return out
    return out


def layer_names():
    p = os.path.join("ProjectSettings", "TagManager.asset")
    names = {}
    if not os.path.isfile(p):
        return names
    try:
        with open(p, encoding="utf-8", errors="replace") as f:
            txt = f.read()
    except OSError:
        return names
    m = re.search(r"(?ms)^\s*layers:\s*\n((?:\s*-.*\n)+)", txt)
    if not m:
        return names
    for i, ln in enumerate(re.findall(r"(?m)^\s*-\s?(.*)$", m.group(1))):
        if ln.strip():
            names[i] = ln.strip()
    return names


PREAMBLE = {"m_ObjectHideFlags", "m_CorrespondingSourceObject", "m_PrefabInstance", "m_PrefabAsset",
            "m_GameObject", "m_Enabled", "m_EditorHideFlags", "m_Script", "m_Name",
            "m_EditorClassIdentifier", "serializedVersion"}


def mono_fields(body):
    """User serialized fields of a MonoBehaviour: body minus standard preamble."""
    out = []
    for ln in body.split("\n"):
        key = re.match(r"^(\s*)([A-Za-z_][\w]*|-)\b", ln)
        top = re.match(r"^  ([A-Za-z_][\w]*):", ln)
        if top and top.group(1) in PREAMBLE:
            continue
        # keep lines that are not part of a skipped preamble block (top-level keys only filtered)
        out.append(ln)
    # drop leading/trailing blanks
    while out and not out[0].strip():
        out.pop(0)
    while out and not out[-1].strip():
        out.pop()
    return out


def main():
    ap = argparse.ArgumentParser(description="Read-only compact dump of a Unity scene/prefab.")
    ap.add_argument("file")
    ap.add_argument("-f", "--fields", action="store_true")
    ap.add_argument("-o", "--object", default=None)
    ap.add_argument("-d", "--depth", type=int, default=None)
    ap.add_argument("--settings", action="store_true")
    ap.add_argument("--deep", action="store_true", help="also scan Library/PackageCache to resolve package scripts (slower)")
    ap.add_argument("--unicode", action="store_true")
    args = ap.parse_args()

    if not os.path.isfile(args.file):
        sys.exit("not found: " + args.file)

    A_ON, A_OFF, A_PF = ("●", "○", "◆") if args.unicode else ("+", "-", "~")
    docs = parse_docs(args.file)
    by_id = {d["id"]: d for d in docs}

    # collect needed guids (scripts + prefab sources) and resolve
    needed = set()
    for d in docs:
        if d["cls"] == 114:
            m = re.search(r"m_Script:\s*\{fileID:\s*-?\d+,\s*guid:\s*([0-9a-fA-F]{32})", d["text"])
            if m:
                needed.add(m.group(1).lower())
        if d["cls"] == 1001:
            m = re.search(r"m_SourcePrefab:\s*\{fileID:\s*-?\d+,\s*guid:\s*([0-9a-fA-F]{32})", d["text"])
            if m:
                needed.add(m.group(1).lower())
    scan_roots = ["Assets", "Packages"] + ([os.path.join("Library", "PackageCache")] if args.deep else [])
    guids = build_guid_map(needed, scan_roots) if needed else {}
    layers = layer_names()

    # index transforms and gameobjects
    transforms = {d["id"]: d for d in docs if d["cls"] in (4, 224) and not d["stripped"]}
    go_of_tr, tr_of_go, children, father = {}, {}, {}, {}
    for tid, d in transforms.items():
        owner = first_fileid(d["text"], "m_GameObject")
        go_of_tr[tid] = owner
        if owner is not None:
            tr_of_go[owner] = tid
        father[tid] = first_fileid(d["text"], "m_Father") or 0
        children[tid] = [int(x) for x in re.findall(r"(?m)^\s*-\s*\{fileID:\s*(-?\d+)\}", d["text"])]

    # prefab instances -> (parent transform id, name, override count)
    prefab_nodes = {}  # parent_tr_id -> list of (name, overrides, pos)
    for d in docs:
        if d["cls"] != 1001:
            continue
        parent_tr = first_fileid(d["text"], "m_TransformParent") or 0
        nm = re.search(r"propertyPath:\s*m_Name\s*\n\s*value:\s*(.*?)\s*\n", d["text"])
        if nm and nm.group(1).strip():
            name = nm.group(1).strip()
        else:
            sm = re.search(r"m_SourcePrefab:\s*\{fileID:\s*-?\d+,\s*guid:\s*([0-9a-fA-F]{32})", d["text"])
            src = guids.get(sm.group(1).lower()) if sm else None
            name = (src[:-7] if src and src.endswith(".prefab") else (src or "prefab"))
        overrides = len(re.findall(r"(?m)^\s*propertyPath:", d["text"]))
        prefab_nodes.setdefault(parent_tr, []).append((name, overrides))

    def go_info(go_id):
        d = by_id.get(go_id)
        if not d:
            return None
        b = d["text"]
        comps = []
        for cid in re.findall(r"(?m)^\s*-\s*component:\s*\{fileID:\s*(-?\d+)\}", b):
            cd = by_id.get(int(cid))
            if not cd or cd["cls"] in (4, 224):
                continue  # transform implied by tree
            if cd["cls"] == 114:
                mm = re.search(r"m_Script:\s*\{fileID:\s*-?\d+,\s*guid:\s*([0-9a-fA-F]{32})", cd["text"])
                if mm:
                    g = mm.group(1).lower()
                    nm = guids.get(g)
                    label = nm[:-3] if nm and nm.endswith(".cs") else (nm or "Script<%s>" % g[:8])
                else:
                    label = "Script?"
                comps.append((label, cd))
            else:
                comps.append((CLASS_NAMES.get(cd["cls"], "u!%d" % cd["cls"]), None))
        nm_raw = scalar(b, "m_Name") or "?"
        if len(nm_raw) >= 2 and nm_raw[0] == nm_raw[-1] and nm_raw[0] in ("'", '"'):
            nm_raw = nm_raw[1:-1]
        return {
            "name": nm_raw,
            "active": (scalar(b, "m_IsActive") or "1") == "1",
            "layer": int(scalar(b, "m_Layer") or 0),
            "tag": scalar(b, "m_TagString") or "Untagged",
            "comps": comps,
        }

    out = []

    def emit_go(tr_id, depth):
        if args.depth is not None and depth > args.depth:
            return
        go_id = go_of_tr.get(tr_id)
        info = go_info(go_id) if go_id is not None else None
        ind = "  " * depth
        if info is None:
            out.append(f"{ind}{A_OFF} <stripped/prefab-child tr {tr_id}>")
        else:
            td = transforms[tr_id]["text"]
            pos = vec(td, "m_LocalPosition") or ("0", "0", "0")
            rot = vec(td, "m_LocalEulerAnglesHint") or None
            scl = vec(td, "m_LocalScale")
            seg = f"{ind}{A_ON if info['active'] else A_OFF} {info['name']} ({pos[0]}, {pos[1]}, {pos[2]})"
            if rot and any(x not in ("0",) for x in rot):
                seg += f" rot({rot[0]}, {rot[1]}, {rot[2]})"
            if scl and tuple(scl) != ("1", "1", "1"):
                seg += f" scl({scl[0]}, {scl[1]}, {scl[2]})"
            extra = []
            if info["tag"] != "Untagged":
                extra.append("tag:" + info["tag"])
            if info["layer"] != 0:
                extra.append("layer:" + layers.get(info["layer"], str(info["layer"])))
            if extra:
                seg += "  {" + ", ".join(extra) + "}"
            if info["comps"]:
                seg += "  [" + ", ".join(c[0] for c in info["comps"]) + "]"
            out.append(seg)
            if args.fields:
                for cname, cd in info["comps"]:
                    if cd is None:
                        continue
                    fl = mono_fields(cd["text"])
                    if fl:
                        out.append(f"{ind}    {cname}:")
                        for ln in fl:
                            out.append(f"{ind}    {ln}")
        # prefab-instance children parented here
        for (pname, ov) in prefab_nodes.get(tr_id, []):
            out.append(f"{ind}  {A_PF} {pname}  [prefab instance, {ov} overrides]")
        # real children, in declared order
        for ch in children.get(tr_id, []):
            if ch in transforms:
                emit_go(ch, depth + 1)

    roots = [tid for tid in transforms if father.get(tid, 0) == 0]
    # stable order: by document appearance
    order = {d["id"]: i for i, d in enumerate(docs)}
    roots.sort(key=lambda t: order.get(t, 0))

    # header
    kind = "Prefab" if args.file.endswith(".prefab") else "Scene"
    ngo = sum(1 for d in docs if d["cls"] == 1 and not d["stripped"])
    npf = sum(1 for d in docs if d["cls"] == 1001)
    out.insert(0, f"{kind}: {os.path.basename(args.file)}   ({ngo} GameObjects, {len(roots)} roots"
                  + (f", {npf} prefab instances" if npf else "") + ")")
    out.insert(1, f"Legend: {A_ON} active  {A_OFF} inactive  {A_PF} prefab   pos always; rot/scl/tag/layer when non-default")
    out.insert(2, "")

    # object filter
    if args.object:
        sub = args.object.lower()
        keep = [tid for tid in transforms
                if (lambda i: i and sub in i["name"].lower())(go_info(go_of_tr.get(tid)))]
        head = out[:3]
        out = head
        for tid in sorted(keep, key=lambda t: order.get(t, 0)):
            emit_go(tid, 0)
        if len(out) == 3:
            out.append(f"(no GameObject name contains '{args.object}')")
    else:
        for r in roots:
            emit_go(r, 0)

    if args.settings:
        out.append("")
        out.append("Settings / non-GameObject docs:")
        for d in docs:
            if d["cls"] in SETTINGS_CLASSES or (d["cls"] != 1 and d["id"] in (1, 2, 3, 4, 5) and d["type"]):
                out.append(f"  - {d['type']} (u!{d['cls']})")

    print("\n".join(out))


if __name__ == "__main__":
    main()
