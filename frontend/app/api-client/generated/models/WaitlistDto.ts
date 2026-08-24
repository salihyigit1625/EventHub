/* generated using openapi-typescript-codegen -- do not edit */
/* istanbul ignore file */
/* tslint:disable */
/* eslint-disable */
import type { WaitlistStatus } from './WaitlistStatus';
export type WaitlistDto = {
    id?: number;
    eventId?: number;
    ticketTypeId?: number;
    attendeeId?: number;
    status?: WaitlistStatus;
    requestedAt?: string;
    notifiedAt?: string | null;
    expiresAt?: string | null;
};

