# TuTuMuch pitch video: media and attribution

| File | What | Source | Licence |
| --- | --- | --- | --- |
| `tutumuch-pitch.mp4`, `tutumuch-pitch.webm` | The 30-second pitch video (H.264/AAC and VP9/Opus) | Built by `docs/mocks/_build/video/build_video.py` | © TuTuMuch |
| `tutumuch-pitch.vtt`, `tutumuch-pitch.txt` | English captions and the plain-text transcript | Same script | © TuTuMuch |
| `tutumuch-pitch-poster.png` | Poster frame (the last slide) | Same script | © TuTuMuch |
| Music | _Upbeat Ukulele Kids_ by Cold_Fire (1:57), an excerpt ducked under the voice | [pixabay.com/music/happy-childrens-tunes-upbeat-ukulele-kids-187874](https://pixabay.com/music/happy-childrens-tunes-upbeat-ukulele-kids-187874/) | [Pixabay Content Licence](https://pixabay.com/service/license-summary/): free for commercial use, no attribution required |
| Narration | Kokoro-82M text-to-speech, voice `af_bella`, speed 1.12 | [huggingface.co/onnx-community/Kokoro-82M-v1.0-ONNX](https://huggingface.co/onnx-community/Kokoro-82M-v1.0-ONNX) (weights from [hexgrad/Kokoro-82M](https://huggingface.co/hexgrad/Kokoro-82M)) | Apache-2.0 |

Visuals use only the TuTuMuch wordmark and design tokens (Ink, Cream, Raspberry, Lilac, Sunny; Giddy's
rounded type and shapes).

The current files are an interim **silent** build (`--silent`): no narration and no music yet, captions
timed from the text. Rebuild with both once the music excerpt and the Kokoro model are available:
`python -I docs/mocks/_build/video/build_video.py` (see `docs/mocks/_build/video/README.md`).
