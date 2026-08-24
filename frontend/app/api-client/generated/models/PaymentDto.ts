/* generated using openapi-typescript-codegen -- do not edit */
/* istanbul ignore file */
/* tslint:disable */
/* eslint-disable */
import type { PaymentStatus } from './PaymentStatus';
export type PaymentDto = {
    id?: number;
    ticketId?: number;
    attendeeId?: number;
    amount?: number;
    status?: PaymentStatus;
    transactionCode?: string;
    createdAt?: string;
};

