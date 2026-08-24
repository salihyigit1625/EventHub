/* generated using openapi-typescript-codegen -- do not edit */
/* istanbul ignore file */
/* tslint:disable */
/* eslint-disable */
import type { AssignGateStaffDto } from '../models/AssignGateStaffDto';
import type { CreateGateStaffDto } from '../models/CreateGateStaffDto';
import type { GateStaffProfileDto } from '../models/GateStaffProfileDto';
import type { GlobalStatsDto } from '../models/GlobalStatsDto';
import type { OrganizerProfileDto } from '../models/OrganizerProfileDto';
import type { PagedOrganizerProfileDto } from '../models/PagedOrganizerProfileDto';
import type { CancelablePromise } from '../core/CancelablePromise';
import { OpenAPI } from '../core/OpenAPI';
import { request as __request } from '../core/request';
export class AdminService {
    /**
     * @param page
     * @param pageSize
     * @returns PagedOrganizerProfileDto OK
     * @throws ApiError
     */
    public static getPendingApprovals(
        page: number = 1,
        pageSize: number = 10,
    ): CancelablePromise<PagedOrganizerProfileDto> {
        return __request(OpenAPI, {
            method: 'GET',
            url: '/api/admin/organizers/pending',
            query: {
                'page': page,
                'pageSize': pageSize,
            },
        });
    }
    /**
     * @param userId
     * @returns OrganizerProfileDto OK
     * @throws ApiError
     */
    public static approveOrganizer(
        userId: number,
    ): CancelablePromise<OrganizerProfileDto> {
        return __request(OpenAPI, {
            method: 'POST',
            url: '/api/admin/organizers/{userId}/approve',
            path: {
                'userId': userId,
            },
        });
    }
    /**
     * @returns GlobalStatsDto OK
     * @throws ApiError
     */
    public static getGlobalStats(): CancelablePromise<GlobalStatsDto> {
        return __request(OpenAPI, {
            method: 'GET',
            url: '/api/admin/stats',
        });
    }
    /**
     * @param requestBody
     * @returns GateStaffProfileDto OK
     * @throws ApiError
     */
    public static createGateStaff(
        requestBody: CreateGateStaffDto,
    ): CancelablePromise<GateStaffProfileDto> {
        return __request(OpenAPI, {
            method: 'POST',
            url: '/api/admin/gate-staff',
            body: requestBody,
            mediaType: 'application/json',
        });
    }
    /**
     * @param requestBody
     * @returns GateStaffProfileDto OK
     * @throws ApiError
     */
    public static assignGateStaff(
        requestBody: AssignGateStaffDto,
    ): CancelablePromise<GateStaffProfileDto> {
        return __request(OpenAPI, {
            method: 'POST',
            url: '/api/admin/gate-staff/assign',
            body: requestBody,
            mediaType: 'application/json',
        });
    }
}
