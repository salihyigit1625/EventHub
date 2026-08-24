/* generated using openapi-typescript-codegen -- do not edit */
/* istanbul ignore file */
/* tslint:disable */
/* eslint-disable */
import type { PagedWaitlistDto } from '../models/PagedWaitlistDto';
import type { TicketDto } from '../models/TicketDto';
import type { WaitlistDto } from '../models/WaitlistDto';
import type { WaitlistStatus } from '../models/WaitlistStatus';
import type { CancelablePromise } from '../core/CancelablePromise';
import { OpenAPI } from '../core/OpenAPI';
import { request as __request } from '../core/request';
export class WaitlistService {
    /**
     * @param ticketTypeId
     * @returns WaitlistDto OK
     * @throws ApiError
     */
    public static joinWaitlist(
        ticketTypeId: number,
    ): CancelablePromise<WaitlistDto> {
        return __request(OpenAPI, {
            method: 'POST',
            url: '/api/waitlist/join/{ticketTypeId}',
            path: {
                'ticketTypeId': ticketTypeId,
            },
        });
    }
    /**
     * @param ticketTypeId
     * @returns WaitlistDto OK
     * @throws ApiError
     */
    public static notifyNextWaitlist(
        ticketTypeId: number,
    ): CancelablePromise<WaitlistDto> {
        return __request(OpenAPI, {
            method: 'POST',
            url: '/api/waitlist/ticket-types/{ticketTypeId}/notify-next',
            path: {
                'ticketTypeId': ticketTypeId,
            },
        });
    }
    /**
     * @param id
     * @returns TicketDto OK
     * @throws ApiError
     */
    public static convertWaitlist(
        id: number,
    ): CancelablePromise<TicketDto> {
        return __request(OpenAPI, {
            method: 'POST',
            url: '/api/waitlist/{id}/convert',
            path: {
                'id': id,
            },
        });
    }
    /**
     * @param page
     * @param pageSize
     * @param status
     * @returns PagedWaitlistDto OK
     * @throws ApiError
     */
    public static getMyWaitlist(
        page: number = 1,
        pageSize: number = 10,
        status?: WaitlistStatus,
    ): CancelablePromise<PagedWaitlistDto> {
        return __request(OpenAPI, {
            method: 'GET',
            url: '/api/waitlist/mine',
            query: {
                'page': page,
                'pageSize': pageSize,
                'status': status,
            },
        });
    }
}
