import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';

import { API_BASE_URL } from '../api-base-url';
import { ApiResult, toResult } from '../api-result';

/** One processed version of a photo: 400, 800 or 1600 px wide, WebP or JPEG (L2-059). */
export interface PhotoVersion {
  size: number;
  width: number;
  height: number;
  format: 'WebP' | 'Jpeg';
  url: string;
}

/** An uploaded listing photo; url is the 400 px WebP thumbnail. */
export interface Photo {
  id: string;
  url: string;
  width: number;
  height: number;
  versions: PhotoVersion[];
}

/** Typed client for POST /api/photo-uploads (upload-listing-photos, L2-017). */
@Injectable({ providedIn: 'root' })
export class PhotosApi {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = inject(API_BASE_URL);

  /** Uploads one file; TuTuMuch API answers 413, 415 or 422 with the message to show. */
  upload(file: File): Promise<ApiResult<Photo>> {
    const body = new FormData();
    body.append('file', file, file.name);
    return toResult(this.http.post<Photo>(`${this.baseUrl}/photo-uploads`, body));
  }
}
