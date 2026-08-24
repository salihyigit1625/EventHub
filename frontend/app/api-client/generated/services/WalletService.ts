/* generated using openapi-typescript-codegen -- do not edit */
/* istanbul ignore file */
/* tslint:disable */
/* eslint-disable */
import type { DepositDto } from '../models/DepositDto';
import type { PagedWalletTransactionDto } from '../models/PagedWalletTransactionDto';
import type { WalletBalanceDto } from '../models/WalletBalanceDto';
import type { CancelablePromise } from '../core/CancelablePromise';
import { OpenAPI } from '../core/OpenAPI';
import { request as __request } from '../core/request';
export class WalletService {
    /**
     * @returns WalletBalanceDto OK
     * @throws ApiError
     */
    public static getWalletBalance(): CancelablePromise<WalletBalanceDto> {
        return __request(OpenAPI, {
            method: 'GET',
            url: '/api/wallet',
        });
    }
    /**
     * @param requestBody
     * @returns WalletBalanceDto OK
     * @throws ApiError
     */
    public static depositWallet(
        requestBody: DepositDto,
    ): CancelablePromise<WalletBalanceDto> {
        return __request(OpenAPI, {
            method: 'POST',
            url: '/api/wallet/deposit',
            body: requestBody,
            mediaType: 'application/json',
        });
    }
    /**
     * @param page
     * @param pageSize
     * @returns PagedWalletTransactionDto OK
     * @throws ApiError
     */
    public static getWalletTransactions(
        page: number = 1,
        pageSize: number = 10,
    ): CancelablePromise<PagedWalletTransactionDto> {
        return __request(OpenAPI, {
            method: 'GET',
            url: '/api/wallet/transactions',
            query: {
                'page': page,
                'pageSize': pageSize,
            },
        });
    }
}
