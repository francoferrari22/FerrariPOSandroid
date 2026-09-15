package com.ferrarispos.manager.data

import okhttp3.Interceptor
import okhttp3.OkHttpClient
import retrofit2.Response
import retrofit2.Retrofit
import retrofit2.converter.gson.GsonConverterFactory
import retrofit2.http.*
import java.util.concurrent.TimeUnit

interface FerrariApi {
    @GET("api/mobile/ping") suspend fun ping(): Response<Map<String,Any>>
    @GET("api/mobile/resumen") suspend fun summary(): Response<Summary>
    @GET("api/mobile/medios-pago") suspend fun paymentMethods(): Response<List<String>>
    @GET("api/mobile/productos") suspend fun products(@Query("q") q:String=""): Response<List<Product>>
    @GET("api/mobile/productos/{id}") suspend fun product(@Path("id") id:Int): Response<Product>
    @POST("api/mobile/productos") suspend fun createProduct(@Body body:ProductWrite): Response<ApiResult>
    @PUT("api/mobile/productos/{id}") suspend fun updateProduct(@Path("id") id:Int,@Body body:ProductWrite): Response<ApiResult>
    @PUT("api/mobile/productos/{id}/precio") suspend fun updatePrice(@Path("id") id:Int,@Body body:PriceUpdate): Response<ApiResult>
    @POST("api/mobile/productos/{id}/stock") suspend fun stock(@Path("id") id:Int,@Body body:StockUpdate): Response<ApiResult>
    @GET("api/mobile/clientes") suspend fun customers(): Response<List<Customer>>
    @POST("api/mobile/clientes") suspend fun createCustomer(@Body body:CustomerWrite): Response<ApiResult>
    @POST("api/mobile/clientes/{id}/abono") suspend fun customerPayment(@Path("id") id:Int,@Body body:CustomerPayment): Response<ApiResult>
    @GET("api/mobile/clientes/{id}/cuenta") suspend fun customerAccount(@Path("id") id:Int): Response<CustomerAccountResponse>
    @GET("api/mobile/ventas") suspend fun sales(): Response<List<Sale>>
    @POST("api/mobile/ventas") suspend fun createSale(@Body body:SaleWrite): Response<SaleResult>
    @POST("api/mobile/ventas-pendientes") suspend fun sendPendingSale(@Body body:SalePendingWrite): Response<ApiResult>
    @GET("api/mobile/ventas/{id}/ticket") suspend fun ticket(@Path("id") id:Long): Response<TicketResult>
    @POST("api/mobile/devoluciones/{saleItemId}") suspend fun returnItem(@Path("saleItemId") saleItemId:Long,@Body body:ReturnWrite): Response<ApiResult>
    @POST("api/mobile/ventas/{id}/cancelar") suspend fun cancelSale(@Path("id") id:Long,@Body body:CancelWrite): Response<ApiResult>
    @GET("api/mobile/caja") suspend fun cash(): Response<CashStatus>
    @GET("api/arqueo") suspend fun arqueo(): Response<CashArqueo>
    @GET("api/mobile/config") suspend fun config(): Response<MobileConfig>
    @POST("api/mobile/caja/apertura") suspend fun openCash(@Body body:CashOpen): Response<ApiResult>
    @POST("api/mobile/caja/movimiento") suspend fun cashMovement(@Body body:CashMovement): Response<ApiResult>
    @POST("api/mobile/caja/cierre") suspend fun closeCash(@Body body:CashClose): Response<ApiResult>
    @GET("api/mobile/promociones") suspend fun promotions(): Response<List<Promotion>>
    @POST("api/mobile/promociones") suspend fun createPromotion(@Body body:PromotionWrite): Response<ApiResult>
    @PUT("api/mobile/promociones") suspend fun updatePromotion(@Body body:PromotionWrite): Response<ApiResult>
    @DELETE("api/mobile/promociones/{id}") suspend fun deletePromotion(@Path("id") id:Long): Response<ApiResult>
    @GET("api/mobile/mesas") suspend fun tables(): Response<List<TableInfo>>
    @GET("api/mobile/tickets-abiertos") suspend fun openTickets(): Response<List<OpenTicket>>
    @POST("api/mobile/tickets-abiertos") suspend fun saveOpenTicket(@Body body:OpenTicket): Response<ApiResult>
    @DELETE("api/mobile/tickets-abiertos/{tableId}") suspend fun deleteOpenTicket(@Path("tableId") tableId:Int): Response<ApiResult>
    @POST("api/mobile/mesas/{tableId}/liberar") suspend fun releaseTable(@Path("tableId") tableId:Int): Response<ApiResult>
    @POST("api/mobile/mesas/{tableId}/cobrar") suspend fun chargeTable(@Path("tableId") tableId:Int,@Body body:TableChargeWrite): Response<SaleResult>
    @POST("api/mobile/tickets-abiertos/{tableId}/agregar") suspend fun appendOpenTicket(@Path("tableId") tableId:Int,@Body body:OpenTicket): Response<ApiResult>
    @GET("api/mobile/analitica") suspend fun analytics(): Response<AnalyticsData>
    @GET("api/mobile/proveedores") suspend fun suppliers(): Response<List<Supplier>>
    @POST("api/mobile/proveedores") suspend fun createSupplier(@Body body:SupplierWrite): Response<ApiResult>
    @GET("api/mobile/compras") suspend fun purchases(): Response<List<Purchase>>
    @POST("api/mobile/compras") suspend fun createPurchase(@Body body:PurchaseWrite): Response<ApiResult>
    @DELETE("api/mobile/compras/{id}") suspend fun deletePurchase(@Path("id") id:Long): Response<ApiResult>
}

class ApiFactory {
    fun create(baseUrl:String,token:String):FerrariApi {
        val auth=Interceptor { chain -> chain.proceed(chain.request().newBuilder().addHeader("X-FerrariPOS-Token",token).build()) }
        val client=OkHttpClient.Builder().addInterceptor(auth).connectTimeout(8,TimeUnit.SECONDS).readTimeout(20,TimeUnit.SECONDS).writeTimeout(20,TimeUnit.SECONDS).build()
        return Retrofit.Builder().baseUrl(if(baseUrl.endsWith("/"))baseUrl else "$baseUrl/").client(client).addConverterFactory(GsonConverterFactory.create()).build().create(FerrariApi::class.java)
    }
}
