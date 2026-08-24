/* generated using openapi-typescript-codegen -- do not edit */
/* istanbul ignore file */
/* tslint:disable */
/* eslint-disable */
import type { EventStatus } from './EventStatus';
import type { TicketTypeDto } from './TicketTypeDto';
export type EventDto = {
    id?: number;
    organizerId?: number;
    title?: string;
    description?: string | null;
    venue?: string;
    startDate?: string;
    endDate?: string;
    cancellationDeadlineHours?: number;
    status?: EventStatus;
    posterDocumentId?: number | null;
    createdAt?: string;
    ticketTypes?: Array<TicketTypeDto>;
};

