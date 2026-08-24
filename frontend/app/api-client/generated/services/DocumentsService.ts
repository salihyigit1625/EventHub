/* generated using openapi-typescript-codegen -- do not edit */
/* istanbul ignore file */
/* tslint:disable */
/* eslint-disable */
import type { DocumentDto } from '../models/DocumentDto';
import type { CancelablePromise } from '../core/CancelablePromise';
import { OpenAPI } from '../core/OpenAPI';
import { request as __request } from '../core/request';
export class DocumentsService {
    /**
     * @returns DocumentDto OK
     * @throws ApiError
     */
    public static getMyDocuments(): CancelablePromise<Array<DocumentDto>> {
        return __request(OpenAPI, {
            method: 'GET',
            url: '/api/documents',
        });
    }
    /**
     * @param formData
     * @returns DocumentDto OK
     * @throws ApiError
     */
    public static uploadDocument(
        formData: {
            file: Blob;
        },
    ): CancelablePromise<DocumentDto> {
        return __request(OpenAPI, {
            method: 'POST',
            url: '/api/documents',
            formData: formData,
            mediaType: 'multipart/form-data',
        });
    }
    /**
     * @param id
     * @returns binary OK
     * @throws ApiError
     */
    public static downloadDocument(
        id: number,
    ): CancelablePromise<Blob> {
        return __request(OpenAPI, {
            method: 'GET',
            url: '/api/documents/{id}',
            path: {
                'id': id,
            },
        });
    }
}
