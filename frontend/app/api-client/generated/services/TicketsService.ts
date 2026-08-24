/* generated using openapi-typescript-codegen -- do not edit */
/* istanbul ignore file */
/* tslint:disable */
/* eslint-disable */
import type { PagedTicketDto } from '../models/PagedTicketDto';
import type { TicketDto } from '../models/TicketDto';
import type { TicketStatus } from '../models/TicketStatus';
import type { CancelablePromise } from '../core/CancelablePromise';
import { OpenAPI } from '../core/OpenAPI';
import { request as __request } from '../core/request';
export class TicketsService {
    /**
     * @param ticketTypeId
     * @returns TicketDto OK
     * @throws ApiError
     */
    public static purchaseTicket(
        ticketTypeId: number,
    ): CancelablePromise<TicketDto> {
        return __request(OpenAPI, {
            method: 'POST',
            url: '/api/tickets/purchase/{ticketTypeId}',
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
    public static cancelTicket(
        id: number,
    ): CancelablePromise<TicketDto> {
        return __request(OpenAPI, {
            method: 'POST',
            url: '/api/tickets/{id}/cancel',
            path: {
                'id': id,
            },
        });
    }
    /**
     * @param page
     * @param pageSize
     * @param status
     * @returns PagedTicketDto OK
     * @throws ApiError
     */
    public static getMyTickets(
        page: number = 1,
        pageSize: number = 10,
        status?: TicketStatus,
    ): CancelablePromise<PagedTicketDto> {
        return __request(OpenAPI, {
            method: 'GET',
            url: '/api/tickets/mine',
            query: {
                'page': page,
                'pageSize': pageSize,
                'status': status,
            },
        });
    }
    /**
     * @param uniqueCode
     * @returns TicketDto OK
     * @throws ApiError
     */
    public static getTicketByCode(
        uniqueCode: string,
    ): CancelablePromise<TicketDto> {
        return __request(OpenAPI, {
            method: 'GET',
            url: '/api/tickets/by-code/{uniqueCode}',
            path: {
                'uniqueCode': uniqueCode,
            },
        });
    }
}
