/* generated using openapi-typescript-codegen -- do not edit */
/* istanbul ignore file */
/* tslint:disable */
/* eslint-disable */
import type { AuthResponseDto } from '../models/AuthResponseDto';
import type { CurrentUserDto } from '../models/CurrentUserDto';
import type { LoginDto } from '../models/LoginDto';
import type { RefreshTokenRequestDto } from '../models/RefreshTokenRequestDto';
import type { RegisterAttendeeDto } from '../models/RegisterAttendeeDto';
import type { RegisterOrganizerDto } from '../models/RegisterOrganizerDto';
import type { CancelablePromise } from '../core/CancelablePromise';
import { OpenAPI } from '../core/OpenAPI';
import { request as __request } from '../core/request';
export class AuthService {
    /**
     * @param requestBody
     * @returns AuthResponseDto OK
     * @throws ApiError
     */
    public static registerAttendee(
        requestBody: RegisterAttendeeDto,
    ): CancelablePromise<AuthResponseDto> {
        return __request(OpenAPI, {
            method: 'POST',
            url: '/api/auth/register/attendee',
            body: requestBody,
            mediaType: 'application/json',
        });
    }
    /**
     * @param requestBody
     * @returns AuthResponseDto OK
     * @throws ApiError
     */
    public static registerOrganizer(
        requestBody: RegisterOrganizerDto,
    ): CancelablePromise<AuthResponseDto> {
        return __request(OpenAPI, {
            method: 'POST',
            url: '/api/auth/register/organizer',
            body: requestBody,
            mediaType: 'application/json',
        });
    }
    /**
     * @param requestBody
     * @returns AuthResponseDto OK
     * @throws ApiError
     */
    public static login(
        requestBody: LoginDto,
    ): CancelablePromise<AuthResponseDto> {
        return __request(OpenAPI, {
            method: 'POST',
            url: '/api/auth/login',
            body: requestBody,
            mediaType: 'application/json',
        });
    }
    /**
     * @param requestBody
     * @returns AuthResponseDto OK
     * @throws ApiError
     */
    public static refreshToken(
        requestBody: RefreshTokenRequestDto,
    ): CancelablePromise<AuthResponseDto> {
        return __request(OpenAPI, {
            method: 'POST',
            url: '/api/auth/refresh',
            body: requestBody,
            mediaType: 'application/json',
        });
    }
    /**
     * @returns void
     * @throws ApiError
     */
    public static logout(): CancelablePromise<void> {
        return __request(OpenAPI, {
            method: 'POST',
            url: '/api/auth/logout',
        });
    }
    /**
     * @returns CurrentUserDto OK
     * @throws ApiError
     */
    public static getCurrentUser(): CancelablePromise<CurrentUserDto> {
        return __request(OpenAPI, {
            method: 'GET',
            url: '/api/auth/me',
        });
    }
}
