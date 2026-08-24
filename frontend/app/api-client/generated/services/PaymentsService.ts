/* generated using openapi-typescript-codegen -- do not edit */
/* istanbul ignore file */
/* tslint:disable */
/* eslint-disable */
import type { PagedPaymentDto } from '../models/PagedPaymentDto';
import type { CancelablePromise } from '../core/CancelablePromise';
import { OpenAPI } from '../core/OpenAPI';
import { request as __request } from '../core/request';
export class PaymentsService {
    /**
     * @param page
     * @param pageSize
     * @returns PagedPaymentDto OK
     * @throws ApiError
     */
    public static getMyPayments(
        page: number = 1,
        pageSize: number = 10,
    ): CancelablePromise<PagedPaymentDto> {
        return __request(OpenAPI, {
            method: 'GET',
            url: '/api/payments/mine',
            query: {
                'page': page,
                'pageSize': pageSize,
            },
        });
    }
}
