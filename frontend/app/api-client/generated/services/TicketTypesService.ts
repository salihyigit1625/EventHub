/* generated using openapi-typescript-codegen -- do not edit */
/* istanbul ignore file */
/* tslint:disable */
/* eslint-disable */
import type { CreateTicketTypeDto } from '../models/CreateTicketTypeDto';
import type { TicketTypeDto } from '../models/TicketTypeDto';
import type { UpdateTicketTypeDto } from '../models/UpdateTicketTypeDto';
import type { CancelablePromise } from '../core/CancelablePromise';
import { OpenAPI } from '../core/OpenAPI';
import { request as __request } from '../core/request';
export class TicketTypesService {
    /**
     * @param eventId
     * @returns TicketTypeDto OK
     * @throws ApiError
     */
    public static getTicketTypesByEvent(
        eventId: number,
    ): CancelablePromise<Array<TicketTypeDto>> {
        return __request(OpenAPI, {
            method: 'GET',
            url: '/api/events/{eventId}/ticket-types',
            path: {
                'eventId': eventId,
            },
        });
    }
    /**
     * @param requestBody
     * @returns TicketTypeDto OK
     * @throws ApiError
     */
    public static createTicketType(
        requestBody: CreateTicketTypeDto,
    ): CancelablePromise<TicketTypeDto> {
        return __request(OpenAPI, {
            method: 'POST',
            url: '/api/ticket-types',
            body: requestBody,
            mediaType: 'application/json',
        });
    }
    /**
     * @param id
     * @param requestBody
     * @returns TicketTypeDto OK
     * @throws ApiError
     */
    public static updateTicketType(
        id: number,
        requestBody: UpdateTicketTypeDto,
    ): CancelablePromise<TicketTypeDto> {
        return __request(OpenAPI, {
            method: 'PUT',
            url: '/api/ticket-types/{id}',
            path: {
                'id': id,
            },
            body: requestBody,
            mediaType: 'application/json',
        });
    }
}
