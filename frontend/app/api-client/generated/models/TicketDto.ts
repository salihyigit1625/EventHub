/* generated using openapi-typescript-codegen -- do not edit */
/* istanbul ignore file */
/* tslint:disable */
/* eslint-disable */
import type { TicketStatus } from './TicketStatus';
export type TicketDto = {
    id?: number;
    ticketTypeId?: number;
    attendeeId?: number;
    uniqueCode?: string;
    unitPrice?: number;
    status?: TicketStatus;
    purchasedAt?: string | null;
    checkedInAt?: string | null;
    createdAt?: string;
};

