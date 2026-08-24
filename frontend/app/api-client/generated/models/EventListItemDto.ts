/* generated using openapi-typescript-codegen -- do not edit */
/* istanbul ignore file */
/* tslint:disable */
/* eslint-disable */
import type { EventStatus } from './EventStatus';
export type EventListItemDto = {
    id?: number;
    organizerId?: number;
    title?: string;
    venue?: string;
    startDate?: string;
    endDate?: string;
    status?: EventStatus;
    posterDocumentId?: number | null;
};

