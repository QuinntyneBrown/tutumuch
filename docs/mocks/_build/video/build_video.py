"""Build the 30-second TuTuMuch pitch video.

Inputs  LINES below                         one narrated line per slide (caption text, spoken text)
        music/upbeat-ukulele-kids-excerpt.mp3  "Upbeat Ukulele Kids" (Cold_Fire, Pixabay), ducked under the voice
        kokoro/                             Kokoro-82M ONNX + af_bella voice from Hugging Face (auto-downloaded)
        docs/design-system/assets/logo.png  the TuTuMuch wordmark
Outputs tutumuch-pitch.mp4 (H.264/AAC), tutumuch-pitch.webm (VP9/Opus), tutumuch-pitch.vtt, tutumuch-pitch.txt,
        tutumuch-pitch-poster.png and ATTRIBUTION.md, written to the app's public media folder
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
TARGET, LEAD, TAIL, MIN_GAP = 30.0, 0.6, 1.8, 0.25  # total length; seconds before the first line and after the last
SILENT_WPS = 2.6  # words per second used to time captions when there is no narration

# (caption and transcript text, text the narrator speaks)
LINES = [
    ("Growing fast? Outgrowing everything? Welcome to TuTuMuch.",
     "Growing fast? Outgrowing everything? Welcome to Tutu Much!"),
    ("New and gently loved clothes and gear, all for girls.",
     "New and gently loved clothes and gear. All for girls."),
    ("Shopping? Clothes, shoes, dance and sports gear, cameras, toys and more. All for girls.",
     "Shopping? Clothes, shoes, dance and sports gear, cameras, toys and more. All for girls!"),
    ("Selling? List in minutes and pass it on to the next girl.",
     "Selling? List in minutes, and pass it on to the next girl."),
    ("Snap a photo. Set a price. Ship it. Get paid.", "Snap a photo. Set a price. Ship it. Get paid!"),
    ("TuTuMuch. Twirl it forward. Come on in.", "Tutu Much. Twirl it forward! Come on in!"),
]

LOGO = ('<img class="logo" alt="" src="data:image/png;base64,'
        + base64.b64encode((REPO / "docs/design-system/assets/logo.png").read_bytes()).decode() + '">')
# The page's decorative shapes: party-pop blobs and a polka-dot band in raspberry, lilac and sunny.
SHAPES = '<div class="shape s1"></div><div class="shape s2"></div><div class="shape s3"></div><div class="dots"></div>'
SLIDES = [
    '<p class="k">Clothes · Dance · Sports · Gear</p><h1>Growing fast?<br>Outgrowing <em>everything?</em></h1>'
    '<p class="sub">Welcome to <b>TuTuMuch</b>.</p>',
    f'{LOGO}<h1 style="margin-top:56px">New and gently loved,<br><em>all for girls.</em></h1>',
    '<p class="k">For shoppers</p><h1>Everything here<br><em>is for girls.</em></h1>'
    '<div class="tags"><span>Clothes</span><span>Shoes</span><span>Dance</span><span>Sports</span><span>Cameras</span><span>Toys</span></div>',
    '<p class="k">For sellers</p><h1>List in minutes.<br><em>Pass it on to the next girl.</em></h1>',
    '<ol class="steps"><li><b>1</b>Snap it</li><li><b>2</b>Price it</li><li><b>3</b>Ship it</li><li><b>4</b>Get paid</li></ol>',
    f'{LOGO}<h1 style="margin-top:56px">Twirl it <em>forward.</em></h1><p class="cta">Come on in → <em>TuTuMuch</em></p>',
]
# TuTuMuch tokens (docs/design-system/tokens): Ink #3a2233 gradient (plum 850 to 700), Cream #fff8f5 text,
# raspberry 300 #f29bbc emphasis, Raspberry #d6336c, Lilac #9b7fd4 and Sunny #f6c64b shapes; Giddy's
# rounded type and pill shapes.
CSS = """@import url('https://fonts.googleapis.com/css2?family=Nunito:wght@600;800;900&display=swap');
html,body{margin:0;width:1920px;height:1080px;overflow:hidden}
body{background:linear-gradient(135deg,#2d1c29 0%,#3a2233 55%,#523d4b 100%);color:#fff8f5;font-family:Nunito,ui-rounded,sans-serif;
  display:flex;flex-direction:column;justify-content:center;align-items:flex-start;padding:0 180px;box-sizing:border-box;position:relative}
h1{font:900 124px/1.04 Nunito,ui-rounded,sans-serif;letter-spacing:-.02em;margin:0;position:relative;z-index:1}
em{font-style:normal;color:#f29bbc}
.k{font:800 40px Nunito,sans-serif;letter-spacing:.04em;text-transform:uppercase;color:#dccbd5;margin:0 0 40px;position:relative;z-index:1}
.sub,.cta{font:800 64px Nunito,sans-serif;margin:48px 0 0;position:relative;z-index:1}.cta em{color:#f29bbc}
.logo{width:760px;position:relative;z-index:1}
.tags{display:flex;gap:28px;margin-top:64px;position:relative;z-index:1}
.tags span{font:800 40px Nunito,sans-serif;padding:18px 36px;border-radius:999px;background:#fde2ea;color:#3a2233}
.steps{list-style:none;display:grid;grid-template-columns:repeat(4,1fr);gap:40px;margin:0;padding:0;width:100%;position:relative;z-index:1}
.steps li{font:900 72px Nunito,sans-serif;letter-spacing:-.02em;border-top:10px solid #d6336c;border-radius:6px;padding-top:36px}
.steps b{display:grid;place-items:center;width:88px;height:88px;border-radius:50%;background:#f6c64b;color:#3a2233;font:900 48px Nunito,sans-serif;margin-bottom:28px}
.shape{position:absolute;z-index:0}
.s1{right:-80px;top:-100px;width:640px;height:540px;background:#d6336c;border-radius:58% 42% 46% 54%/52% 44% 56% 48%}
.s2{right:300px;bottom:-130px;width:380px;height:380px;background:#9b7fd4;border-radius:50%}
.s3{right:90px;bottom:150px;width:180px;height:180px;background:#f6c64b;border-radius:50%;opacity:.95}
.dots{position:absolute;left:0;bottom:12px;width:100%;height:28px;background:radial-gradient(circle,#f29bbc 30%,transparent 32%) 0 50%/56px 28px repeat-x;opacity:.9}"""


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
        for sub, name in (("onnx", "model.onnx"), ("voices", f"{VOICE}.bin")):
            if not (k / name).exists():
                shutil.copy(hf_hub_download(HF_REPO, f"{sub}/{name}"), k / name)
        self.tok = Tokenizer()
        self.sess = ort.InferenceSession(str(k / "model.onnx"))
        self.style = np.fromfile(k / f"{VOICE}.bin", dtype=np.float32).reshape(-1, 1, 256)

    def say(self, text, out):
        np = self.np
        ids = self.tok.tokenize(self.tok.phonemize(text, lang="en-us"))
        audio = self.sess.run(None, {"input_ids": np.array([[0, *ids, 0]], dtype=np.int64),
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

    # Timeline: the gaps between lines stretch so the video lands on TARGET seconds.
    speech = sum(d for _, d in voices)
    gap = (TARGET - LEAD - TAIL - speech) / (len(voices) - 1)
    if gap < MIN_GAP:
        raise SystemExit(f"narration is {speech:.1f}s; shorten LINES or raise SPEED to fit {TARGET:.0f}s")
    starts, t = [], LEAD
    for _, d in voices:
        starts.append(t)
        t += d + gap
    total = TARGET
    slide_d = [(starts[i + 1] if i + 1 < len(starts) else total) - (0 if i == 0 else starts[i]) for i in range(len(starts))]

    with sync_playwright() as pw:
        b = pw.chromium.launch()
        pg = b.new_page(viewport={"width": 1920, "height": 1080})
        for i, html in enumerate(SLIDES):
            pg.set_content(f"<style>{CSS}</style>{SHAPES if i in (0, 2, 4, 5) else ''}{html}")
            pg.wait_for_timeout(900)
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
    shutil.copy(tmp / "s5.png", out / f"{NAME}-poster.png")

    vtt = ["WEBVTT", ""]
    for i, ((caption, _), (_, d)) in enumerate(zip(LINES, voices)):
        vtt += [str(i + 1), f"{ts(starts[i])} --> {ts(starts[i] + d)}", caption, ""]
    (out / f"{NAME}.vtt").write_text("\n".join(vtt), encoding="utf-8")
    (out / f"{NAME}.txt").write_text("TuTuMuch in 30 seconds: transcript\n\n" + "\n".join(c for c, _ in LINES) + "\n", encoding="utf-8")
    shutil.copy(HERE / "ATTRIBUTION.md", out / "ATTRIBUTION.md")
    for other in OUT_DIRS[1:]:
        other.mkdir(parents=True, exist_ok=True)
        for f in out.iterdir():
            shutil.copy(f, other / f.name)
    print(f"built {total:.1f}s video ({'silent' if silent else 'narrated'}, {'with' if music else 'no'} music), "
          f"speech {speech:.1f}s, gap {gap:.2f}s")


if __name__ == "__main__":
    main()
