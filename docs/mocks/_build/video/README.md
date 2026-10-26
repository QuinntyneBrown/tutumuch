# TuTuMuch pitch video build

`build_video.py` builds the 30-second pitch video that plays on the marketing page (`/welcome`).
The narration lines, the slide markup and the TuTuMuch-token slide styles are in the script.

```
python -I docs/mocks/_build/video/build_video.py            # narration and music
python -I docs/mocks/_build/video/build_video.py --silent   # no narration; music only if present
```

It writes `tutumuch-pitch.mp4`, `tutumuch-pitch.webm`, `tutumuch-pitch.vtt`, `tutumuch-pitch.txt`,
`tutumuch-pitch-poster.png` and `ATTRIBUTION.md` to `src/frontend/projects/tutumuch/public/media/`
(served at `/media/`) and copies them to `docs/mocks/assets/media/` for the mocks.

Needs ffmpeg and ffprobe and the Python package `playwright` (with `python -m playwright install
chromium`); narration also needs `kokoro-onnx`, `onnxruntime`, `huggingface_hub` and `numpy`.

- **Voice**: Kokoro-82M from Hugging Face, voice `af_bella` at speed 1.12. The model and voice
  download into `kokoro/` (git-ignored, about 330 MB) on the first run.
- **Music**: `music/upbeat-ukulele-kids-excerpt.mp3` is a 34-second excerpt of _Upbeat Ukulele
  Kids_ by Cold_Fire on Pixabay (Pixabay Content Licence), committed so the build is repeatable.
  It was cut from the track on its Pixabay page, starting on the beat at 4.25 s, with
  `ffmpeg -ss 4.25 -t 34 -i upbeat-ukulele-kids-187874.mp3 -af "afade=t=out:st=32:d=2" -b:a 160k music/upbeat-ukulele-kids-excerpt.mp3`.
  A sidechain compressor ducks it under the voice.
- **Length**: the gaps between lines stretch so the video is exactly 30 s. The script stops if
  the narration no longer fits.
- **Wording**: everything is for girls; the copy never promises boys' or adult sizes
  ([ADR general/0005](../../../adr/general/0005-marketing-wording.md)).

Sources and licences: [`ATTRIBUTION.md`](ATTRIBUTION.md).
