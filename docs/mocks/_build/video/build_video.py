"""Build the short TuTuMuch pitch video: four slides, no filler (L2-080).

The video speaks to shoppers only: in launch Phase 1 (the default) only TuTuMuch sells, so it
promises no member selling (L2-094, ADR platform/0014). The slides follow the page guidelines
(docs/page-guidelines.md, ADR general/0009): cream and blush, Fraunces and DM Sans, Tilly, no
gradients or decorative shapes.

Inputs  LINES below                         one narrated line per slide (caption text, spoken text)
        music/upbeat-ukulele-kids-excerpt.mp3  "Upbeat Ukulele Kids" (Cold_Fire, Pixabay), ducked under the voice
        kokoro/                             Kokoro-82M ONNX + af_bella voice from Hugging Face (auto-downloaded;
                                            the kokoro-onnx GitHub release when Hugging Face is unreachable)
        docs/design-system/assets/logo.png  the TuTuMuch wordmark
        docs/design-system/assets/fonts/    Fraunces and DM Sans (OFL)
        docs/design-system/assets/tilly/    Tilly's poses
Outputs tutumuch-pitch.mp4 (H.264/AAC), tutumuch-pitch.webm (VP9/Opus), tutumuch-pitch.vtt,
        tutumuch-pitch-poster.png, tutumuch-share.jpg (1200x630 link preview) and ATTRIBUTION.md, written to the app's public media folder
        (src/frontend/projects/tutumuch/public/media/) and copied to docs/mocks/assets/media/.

Needs: ffmpeg/ffprobe, python playwright (chromium); for narration also kokoro-onnx, onnxruntime,
huggingface_hub and numpy.
Run:   python -I docs/mocks/_build/video/build_video.py            full build: narration and music
       python -I docs/mocks/_build/video/build_video.py --silent   no narration; music only if the file exists
"""
import base64, pathlib, shutil, subprocess, sys, tempfile, wave

from playwright.sync_api import sync_playwright

HERE = pathlib.Path(__file__).resolve().parent
REPO = HERE.parents[3]
OUT_DIRS = [REPO / "src/frontend/projects/tutumuch/public/media", REPO / "docs/mocks/assets/media"]
NAME = "tutumuch-pitch"
HF_REPO, VOICE, SPEED = "onnx-community/Kokoro-82M-v1.0-ONNX", "af_bella", 1.12
MUSIC, MUSIC_DB = HERE / "music/upbeat-ukulele-kids-excerpt.mp3", -9
LEAD, GAP, TAIL = 0.5, 0.45, 1.4  # seconds before the first line, between lines and after the last; the length follows the narration
SILENT_WPS = 2.6  # words per second used to time captions when there is no narration

# (caption text, text the narrator speaks). Shopping only: nothing promises that a member can sell.
# One line per slide, nothing that only fills time.
LINES = [
    ("Outgrown already? Welcome to TuTuMuch.", "Outgrown already? Welcome to Tutu Much!"),
    ("New and gently loved clothes, shoes, dance and sports gear for girls.",
     "New and gently loved clothes, shoes, dance and sports gear for girls."),
    ("Every piece hand-picked, its condition named up front.", "Every piece hand-picked, its condition named up front."),
    ("Find it, buy it, it ships to you. Shop the closet.", "Find it, buy it, it ships to you. Shop the closet!"),
]

ASSETS = REPO / "docs/design-system/assets"


def data_uri(path, mime):
    return f"data:{mime};base64," + base64.b64encode(path.read_bytes()).decode()


LOGO = f'<img class="logo" alt="" src="{data_uri(ASSETS / "logo.png", "image/png")}">'


def tilly(pose, cls):
    return f'<img class="tilly {cls}" alt="" src="{data_uri(ASSETS / "tilly" / f"tilly-{pose}.webp", "image/webp")}">'


# One Tilly per slide at most, in an approved pose (page guidelines 6).
SLIDES = [
    '<div class="copy"><h1>Outgrown<br><em>already?</em></h1><p class="sub">Welcome to <b>TuTuMuch</b>.</p></div>'
    + tilly("hello", "right"),
    '<div class="copy"><p class="k">New and gently loved</p><h1>Everything here<br><em>is for girls.</em></h1>'
    '<div class="tags"><span>Clothes</span><span>Shoes</span><span>Dance</span><span>Sports</span><span>Cameras</span><span>Toys</span></div></div>'
    + tilly("soccer", "right"),
    '<div class="copy"><p class="k">Hand-picked by TuTuMuch</p><h1>Honest about<br><em>condition.</em></h1>'
    '<div class="badges"><span class="new">New with tags</span><span class="like">Used - Excellent</span>'
    '<span class="loved">Used - Good</span><span class="well">Used - Fair</span></div></div>',
    f'<div class="copy">{LOGO}<h1 style="margin-top:48px">Find it. Buy it.<br><em>It ships to you.</em></h1>'
    '<p class="cta">Shop the closet →</p></div>' + tilly("twirl", "right"),
]
BLUSH = {0}  # slides on a blush panel: sections change background, never use gradients
# Page-guideline tokens (docs/design-system/tokens/tokens.css): Cream #fff8f5 page, Blush #fde2ea panels,
# Ink #3a2233 text, Muted #7a6370, raspberry 700 #c2255c emphasis, Raspberry #d6336c pills, Sunny #f6c64b
# and the condition-badge colours; Fraunces (SOFT 100) for headings, DM Sans for text; no gradients or shapes.
FONTS = (f"@font-face{{font-family:Fraunces;font-weight:100 900;src:url({data_uri(ASSETS / 'fonts/fraunces-latin-full.woff2', 'font/woff2')})}}"
         f"@font-face{{font-family:'DM Sans';font-weight:100 1000;src:url({data_uri(ASSETS / 'fonts/dm-sans-latin-opsz.woff2', 'font/woff2')})}}")
CSS = FONTS + """
html,body{margin:0;width:1920px;height:1080px;overflow:hidden}
body{background:#fff8f5;color:#3a2233;font-family:'DM Sans',sans-serif;display:flex;align-items:center;
  padding:0 180px;box-sizing:border-box;position:relative}
body.blush{background:#fde2ea}
.copy{position:relative;z-index:1;max-width:1000px}
h1{font:600 112px/1.04 Fraunces,serif;font-variation-settings:'SOFT' 100,'WONK' 0;letter-spacing:-.025em;margin:0}
em{font-style:normal;color:#c2255c}
.k{font:600 40px 'DM Sans',sans-serif;color:#7a6370;margin:0 0 40px}
.sub{font:500 60px 'DM Sans',sans-serif;margin:48px 0 0}
.cta{display:inline-block;font:600 56px 'DM Sans',sans-serif;margin:56px 0 0;padding:28px 64px;border-radius:999px;background:#d6336c;color:#fff}
.logo{width:760px}
.tilly{position:absolute;z-index:0;height:760px;width:auto}
.tilly.right{right:120px;bottom:160px}
.tags,.badges{display:flex;flex-wrap:wrap;gap:24px;margin-top:64px}
.tags span,.badges span{font:600 40px 'DM Sans',sans-serif;padding:18px 36px;border-radius:999px}
.tags span{background:#fde2ea;color:#3a2233}
.badges .new{background:#d6336c;color:#fff}.badges .like{background:#f6c64b;color:#3a2233}
.badges .loved{background:#fde2ea;color:#3a2233}.badges .well{background:#fff;color:#3a2233;box-shadow:inset 0 0 0 3px rgb(58 34 51/.15)}"""


def dur(p):
    return float(subprocess.check_output(["ffprobe", "-v", "error", "-show_entries", "format=duration",
                                          "-of", "csv=p=0", str(p)]).strip())


def ts(t):
    return f"{int(t // 3600):02}:{int(t % 3600 // 60):02}:{t % 60:06.3f}"


class Narrator:
    """Kokoro-82M via onnxruntime; model and voice come from Hugging Face on first run."""

    def __init__(self):
        import numpy as np
        import onnxruntime as ort
        from huggingface_hub import hf_hub_download
        from kokoro_onnx.tokenizer import Tokenizer

        self.np = np
        k = HERE / "kokoro"
        k.mkdir(exist_ok=True)
        try:
            for sub, name in (("onnx", "model.onnx"), ("voices", f"{VOICE}.bin")):
                if not (k / name).exists():
                    shutil.copy(hf_hub_download(HF_REPO, f"{sub}/{name}"), k / name)
        except Exception as e:  # Hugging Face unreachable: the same Kokoro-82M v1.0 weights from the kokoro-onnx release
            print(f"note: Hugging Face download failed ({type(e).__name__}); using the kokoro-onnx GitHub release")
            self.from_github(k)
        self.tok = Tokenizer()
        self.sess = ort.InferenceSession(str(k / "model.onnx"))
        self.input = self.sess.get_inputs()[0].name  # "input_ids" (Hugging Face export) or "tokens" (kokoro-onnx release)
        self.style = np.fromfile(k / f"{VOICE}.bin", dtype=np.float32).reshape(-1, 1, 256)

    def from_github(self, k):
        import urllib.request

        base = "https://github.com/thewh1teagle/kokoro-onnx/releases/download/model-files-v1.0/"
        if not (k / "model.onnx").exists():
            urllib.request.urlretrieve(base + "kokoro-v1.0.onnx", k / "model.onnx")
        if not (k / f"{VOICE}.bin").exists():
            urllib.request.urlretrieve(base + "voices-v1.0.bin", k / "voices-v1.0.npz")
            self.np.load(k / "voices-v1.0.npz")[VOICE].astype(self.np.float32).tofile(k / f"{VOICE}.bin")

    def say(self, text, out):
        np = self.np
        ids = self.tok.tokenize(self.tok.phonemize(text, lang="en-us"))
        audio = self.sess.run(None, {self.input: np.array([[0, *ids, 0]], dtype=np.int64),
                                     "style": self.style[len(ids)],
                                     "speed": np.array([SPEED], dtype=np.float32)})[0].reshape(-1)
        audio = np.trim_zeros(np.where(np.abs(audio) < 0.004, 0, audio))  # trim leading/trailing silence
        with wave.open(str(out), "wb") as w:
            w.setnchannels(1); w.setsampwidth(2); w.setframerate(24000)
            w.writeframes((np.clip(audio, -1, 1) * 32767).astype(np.int16).tobytes())


def main():
    silent = "--silent" in sys.argv[1:]
    music = MUSIC.exists()
    if not music:
        print(f"note: {MUSIC.name} is missing, building without music (see README.md for the track and how to cut it)")
    tmp = pathlib.Path(tempfile.mkdtemp())
    voices = []
    if silent:
        voices = [(None, len(caption.split()) / SILENT_WPS) for caption, _ in LINES]
    else:
        narrator = Narrator()
        for i, (_, spoken) in enumerate(LINES):
            out = tmp / f"v{i}.wav"
            narrator.say(spoken, out)
            voices.append((out, dur(out)))

    # Timeline: short fixed gaps; the video is as long as the narration needs.
    speech = sum(d for _, d in voices)
    gap = GAP
    starts, t = [], LEAD
    for _, d in voices:
        starts.append(t)
        t += d + gap
    total = round(t - gap + TAIL, 3)
    if total >= 30:
        raise SystemExit(f"the video would be {total:.1f}s; L2-080 asks for under 30 s, so shorten LINES")
    slide_d = [(starts[i + 1] if i + 1 < len(starts) else total) - (0 if i == 0 else starts[i]) for i in range(len(starts))]

    with sync_playwright() as pw:
        b = pw.chromium.launch()
        pg = b.new_page(viewport={"width": 1920, "height": 1080})
        for i, html in enumerate(SLIDES):
            pg.set_content(f"<style>{CSS}</style><body class=\"{'blush' if i in BLUSH else ''}\">{html}</body>")
            pg.evaluate("document.fonts.ready")
            pg.wait_for_timeout(600)
            pg.screenshot(path=str(tmp / f"s{i}.png"))
        b.close()

    inputs, filt, n = [], [], 0
    if not silent:
        # Narration placed at its start times, music ducked under it by a sidechain compressor.
        for i, (p, _) in enumerate(voices):
            inputs += ["-i", str(p)]
            ms = int(starts[i] * 1000)
            filt.append(f"[{i}:a]aresample=48000,adelay={ms}|{ms}[a{i}]")
        n = len(voices)
        mix = "".join(f"[a{i}]" for i in range(n)) + f"amix=inputs={n}:normalize=0,apad,atrim=0:{total:.3f},volume=1.7"
        filt.append(f"{mix},asplit=2[vx][key]" if music else f"{mix},alimiter=limit=0.95[aout]")
    if music:
        inputs += ["-i", str(MUSIC)]
        bg = (f"[{n}:a]aresample=48000,atrim=0:{total:.3f},asetpts=PTS-STARTPTS,volume={MUSIC_DB}dB,"
              f"afade=t=in:d=0.6,afade=t=out:st={total - 2.0:.3f}:d=2.0")
        if silent:
            filt.append(f"{bg},alimiter=limit=0.95[aout]")
        else:
            filt.append(f"{bg}[bg0];[bg0][key]sidechaincompress=threshold=0.03:ratio=6:attack=15:release=350[bg];"
                        "[vx][bg]amix=inputs=2:normalize=0,alimiter=limit=0.95[aout]")
        n += 1
    elif silent:
        inputs += ["-f", "lavfi", "-t", f"{total:.3f}", "-i", "anullsrc=r=48000:cl=stereo"]
        filt.append(f"[{n}:a]anull[aout]")
        n += 1

    # Video: slides with short crossfades.
    for i in range(len(SLIDES)):
        inputs += ["-loop", "1", "-framerate", "30", "-t", f"{slide_d[i] + 0.4:.3f}", "-i", str(tmp / f"s{i}.png")]
    prev, off = f"[{n}:v]", 0.0
    for i in range(1, len(SLIDES)):
        off += slide_d[i - 1]
        filt.append(f"{prev}[{n + i}:v]xfade=transition=slideleft:duration=0.35:offset={off - 0.2:.3f}[x{i}]")
        prev = f"[x{i}]"
    filt.append(f"{prev}fade=t=in:d=0.3,fade=t=out:st={total - 0.5:.3f}:d=0.5,format=yuv420p[v1]")
    graph = ";".join(filt)

    out = OUT_DIRS[0]
    out.mkdir(parents=True, exist_ok=True)
    subprocess.check_call(["ffmpeg", "-y", "-loglevel", "error", *inputs, "-filter_complex", graph,
                           "-map", "[v1]", "-map", "[aout]", "-t", f"{total:.3f}", "-r", "30",
                           "-c:v", "libx264", "-preset", "slow", "-crf", "24", "-tune", "stillimage",
                           "-c:a", "aac", "-b:a", "128k", "-movflags", "+faststart", str(out / f"{NAME}.mp4")])
    subprocess.check_call(["ffmpeg", "-y", "-loglevel", "error", *inputs, "-filter_complex", graph,
                           "-map", "[v1]", "-map", "[aout]", "-t", f"{total:.3f}", "-r", "30",
                           "-c:v", "libvpx-vp9", "-b:v", "0", "-crf", "38", "-row-mt", "1", "-deadline", "good",
                           "-c:a", "libopus", "-b:a", "96k", str(out / f"{NAME}.webm")])
    shutil.copy(tmp / f"s{len(SLIDES) - 1}.png", out / f"{NAME}-poster.png")

    vtt = ["WEBVTT", ""]
    for i, ((caption, _), (_, d)) in enumerate(zip(LINES, voices)):
        vtt += [str(i + 1), f"{ts(starts[i])} --> {ts(starts[i] + d)}", caption, ""]
    (out / f"{NAME}.vtt").write_text("\n".join(vtt), encoding="utf-8")
    # Link-preview image for the og:image and twitter:image tags: the poster cut to 1200x630.
    subprocess.check_call(["ffmpeg", "-y", "-loglevel", "error", "-i", str(out / f"{NAME}-poster.png"),
                           "-vf", "scale=1200:-1,crop=1200:630", "-q:v", "3", str(out / "tutumuch-share.jpg")])
    shutil.copy(HERE / "ATTRIBUTION.md", out / "ATTRIBUTION.md")
    for other in OUT_DIRS[1:]:
        other.mkdir(parents=True, exist_ok=True)
        for f in out.iterdir():
            shutil.copy(f, other / f.name)
    print(f"built {total:.1f}s video ({'silent' if silent else 'narrated'}, {'with' if music else 'no'} music), "
          f"speech {speech:.1f}s, gap {gap:.2f}s")


if __name__ == "__main__":
    main()
