/* generated using openapi-typescript-codegen -- do not edit */
/* istanbul ignore file */
/* tslint:disable */
/* eslint-disable */
import type { CheckInResultDto } from '../models/CheckInResultDto';
import type { CheckInTicketDto } from '../models/CheckInTicketDto';
import type { GateStaffProfileDto } from '../models/GateStaffProfileDto';
import type { CancelablePromise } from '../core/CancelablePromise';
import { OpenAPI } from '../core/OpenAPI';
import { request as __request } from '../core/request';
export class GateStaffService {
    /**
     * @param requestBody
     * @returns CheckInResultDto OK
     * @throws ApiError
     */
    public static checkInTicket(
        requestBody: CheckInTicketDto,
    ): CancelablePromise<CheckInResultDto> {
        return __request(OpenAPI, {
            method: 'POST',
            url: '/api/gate-staff/check-in',
            body: requestBody,
            mediaType: 'application/json',
        });
    }
    /**
     * @returns GateStaffProfileDto OK
     * @throws ApiError
     */
    public static getAssignedEvent(): CancelablePromise<GateStaffProfileDto> {
        return __request(OpenAPI, {
            method: 'GET',
            url: '/api/gate-staff/assigned-event',
        });
    }
}
