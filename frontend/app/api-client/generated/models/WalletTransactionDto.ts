/* generated using openapi-typescript-codegen -- do not edit */
/* istanbul ignore file */
/* tslint:disable */
/* eslint-disable */
import type { WalletTransactionType } from './WalletTransactionType';
export type WalletTransactionDto = {
    id?: number;
    attendeeId?: number;
    amount?: number;
    balanceAfter?: number;
    type?: WalletTransactionType;
    paymentId?: number | null;
    ticketId?: number | null;
    createdAt?: string;
};

