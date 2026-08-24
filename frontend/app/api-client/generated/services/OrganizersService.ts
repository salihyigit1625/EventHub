/* generated using openapi-typescript-codegen -- do not edit */
/* istanbul ignore file */
/* tslint:disable */
/* eslint-disable */
import type { OrganizerProfileDto } from '../models/OrganizerProfileDto';
import type { UpdateOrganizerProfileDto } from '../models/UpdateOrganizerProfileDto';
import type { CancelablePromise } from '../core/CancelablePromise';
import { OpenAPI } from '../core/OpenAPI';
import { request as __request } from '../core/request';
export class OrganizersService {
    /**
     * @param userId
     * @returns OrganizerProfileDto OK
     * @throws ApiError
     */
    public static getOrganizerProfile(
        userId: number,
    ): CancelablePromise<OrganizerProfileDto> {
        return __request(OpenAPI, {
            method: 'GET',
            url: '/api/organizers/{userId}',
            path: {
                'userId': userId,
            },
        });
    }
    /**
     * @returns OrganizerProfileDto OK
     * @throws ApiError
     */
    public static getMyOrganizerProfile(): CancelablePromise<OrganizerProfileDto> {
        return __request(OpenAPI, {
            method: 'GET',
            url: '/api/organizers/me',
        });
    }
    /**
     * @param requestBody
     * @returns OrganizerProfileDto OK
     * @throws ApiError
     */
    public static updateOrganizerProfile(
        requestBody: UpdateOrganizerProfileDto,
    ): CancelablePromise<OrganizerProfileDto> {
        return __request(OpenAPI, {
            method: 'PUT',
            url: '/api/organizers/me',
            body: requestBody,
            mediaType: 'application/json',
        });
    }
}
