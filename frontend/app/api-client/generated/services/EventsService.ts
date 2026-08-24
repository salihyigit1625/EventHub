/* generated using openapi-typescript-codegen -- do not edit */
/* istanbul ignore file */
/* tslint:disable */
/* eslint-disable */
import type { CreateEventDto } from '../models/CreateEventDto';
import type { EventDto } from '../models/EventDto';
import type { EventStatus } from '../models/EventStatus';
import type { PagedEventListItemDto } from '../models/PagedEventListItemDto';
import type { UpdateEventDto } from '../models/UpdateEventDto';
import type { CancelablePromise } from '../core/CancelablePromise';
import { OpenAPI } from '../core/OpenAPI';
import { request as __request } from '../core/request';
export class EventsService {
    /**
     * @param page
     * @param pageSize
     * @param search
     * @param venue
     * @param from
     * @param to
     * @param status
     * @returns PagedEventListItemDto OK
     * @throws ApiError
     */
    public static getPublishedEvents(
        page: number = 1,
        pageSize: number = 10,
        search?: string,
        venue?: string,
        from?: string,
        to?: string,
        status?: EventStatus,
    ): CancelablePromise<PagedEventListItemDto> {
        return __request(OpenAPI, {
            method: 'GET',
            url: '/api/events',
            query: {
                'page': page,
                'pageSize': pageSize,
                'search': search,
                'venue': venue,
                'from': from,
                'to': to,
                'status': status,
            },
        });
    }
    /**
     * @param requestBody
     * @returns EventDto Created
     * @throws ApiError
     */
    public static createEvent(
        requestBody: CreateEventDto,
    ): CancelablePromise<EventDto> {
        return __request(OpenAPI, {
            method: 'POST',
            url: '/api/events',
            body: requestBody,
            mediaType: 'application/json',
        });
    }
    /**
     * @param id
     * @returns EventDto OK
     * @throws ApiError
     */
    public static getEventById(
        id: number,
    ): CancelablePromise<EventDto> {
        return __request(OpenAPI, {
            method: 'GET',
            url: '/api/events/{id}',
            path: {
                'id': id,
            },
        });
    }
    /**
     * @param id
     * @param requestBody
     * @returns EventDto OK
     * @throws ApiError
     */
    public static updateEvent(
        id: number,
        requestBody: UpdateEventDto,
    ): CancelablePromise<EventDto> {
        return __request(OpenAPI, {
            method: 'PUT',
            url: '/api/events/{id}',
            path: {
                'id': id,
            },
            body: requestBody,
            mediaType: 'application/json',
        });
    }
    /**
     * @param page
     * @param pageSize
     * @param search
     * @param venue
     * @param from
     * @param to
     * @param status
     * @returns PagedEventListItemDto OK
     * @throws ApiError
     */
    public static getMyEvents(
        page: number = 1,
        pageSize: number = 10,
        search?: string,
        venue?: string,
        from?: string,
        to?: string,
        status?: EventStatus,
    ): CancelablePromise<PagedEventListItemDto> {
        return __request(OpenAPI, {
            method: 'GET',
            url: '/api/events/mine',
            query: {
                'page': page,
                'pageSize': pageSize,
                'search': search,
                'venue': venue,
                'from': from,
                'to': to,
                'status': status,
            },
        });
    }
    /**
     * @param id
     * @returns EventDto OK
     * @throws ApiError
     */
    public static publishEvent(
        id: number,
    ): CancelablePromise<EventDto> {
        return __request(OpenAPI, {
            method: 'POST',
            url: '/api/events/{id}/publish',
            path: {
                'id': id,
            },
        });
    }
    /**
     * @param id
     * @returns EventDto OK
     * @throws ApiError
     */
    public static cancelEvent(
        id: number,
    ): CancelablePromise<EventDto> {
        return __request(OpenAPI, {
            method: 'POST',
            url: '/api/events/{id}/cancel',
            path: {
                'id': id,
            },
        });
    }
    /**
     * @param id
     * @returns binary OK
     * @throws ApiError
     */
    public static getEventPoster(
        id: number,
    ): CancelablePromise<Blob> {
        return __request(OpenAPI, {
            method: 'GET',
            url: '/api/events/{id}/poster',
            path: {
                'id': id,
            },
        });
    }
    /**
     * @param id
     * @param formData
     * @returns EventDto OK
     * @throws ApiError
     */
    public static uploadEventPoster(
        id: number,
        formData: {
            file: Blob;
        },
    ): CancelablePromise<EventDto> {
        return __request(OpenAPI, {
            method: 'POST',
            url: '/api/events/{id}/poster',
            path: {
                'id': id,
            },
            formData: formData,
            mediaType: 'multipart/form-data',
        });
    }
}
