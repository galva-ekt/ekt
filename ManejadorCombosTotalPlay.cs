using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Elektra.Negocio.Entidades.CombosTotalPlay;
using Elektra.Negocio.Entidades.CombosTotalPlay.EntidadesBanco;
using EntNEw= Elektra.Negocio.Entidades.NewServicioTienda;
using Elektra.Negocio.CombosTotalPlay.Controlador;
using Elektra.Negocio.NewServicioTienda;
using ApiC = Elektra.Negocio.APICredito;
using System.Data;
using System.Collections;
using ElektraNegocioEntidades.NewServicioTienda;
using Elektra.Negocio.Entidades.Tienda;
using Elektra.Negocio.Entidades.Ventas;

namespace Elektra.Negocio.CombosTotalPlay
{

    public class ManejadorCombosTotalPlay
    {
        private Hashtable EngancheAplicadoProrateo;
        #region variables
        private LogsManager log = null;
        #endregion
        #region constructores

        /// <summary>
        /// Constructor por defecto
        /// </summary>
        public ManejadorCombosTotalPlay()
        {
            log = new LogsManager();
        }
         #endregion

        #region MetodosPublicos
        public EntListaProductos obtenerProductosTP() {
            return new CtrlCombosTotalPlay().obtenerProductos();
        }

        public EntListaPaquetes obtenerPaquetesTP() {
            return new CtrlCombosTotalPlay().obtenerPaquetes();
        }

        public EntFamiliasSugeridas obtenerFamiliasSugeridasTP() {
            return new CtrlCombosTotalPlay().obtenerFamiliasSugeridas();
        }

        public EntProdSugeridos obtenerProdSugeridosTP(int familia) {
            return new CtrlCombosTotalPlay().obtenerProdSugeridos(familia);
        }

        public EntResponseTPlay guardaPersonaTPlay(EntPersonaTPlay persona)
        {
            return new CtrlCombosTotalPlay().guardaPersonaTPlay(persona);
        }
        public EntResponseTPlay guardaDomicilioTPlay(EntDomicilioClienteTPlay domicilio)
        {
            return new CtrlCombosTotalPlay().guardaDomicilioTPlay(domicilio);
        }
        public EntResponseTPlay guardaVentaTPlay(EntVentaTPlay infoventa)
        {
            return new CtrlCombosTotalPlay().guardaVentaTPlay(infoventa);
        }
        public EntResponseTPlay guardaAllTPlay(EntPersonaTPlay persona, EntDomicilioClienteTPlay domicilio, EntVentaTPlay infoVenta)
        {
            EntResponseTPlay response = new EntResponseTPlay();
            EntVentaTPlay infoVentaAux = infoVenta;
            int personaIdAux = 0, domicilioIdAux = 0;
            //Guardamos el cliente
            response = guardaPersonaTPlay(persona);
            if (response.personaId > 0 && !response.EstadoOperacion.Status)
            {
                personaIdAux = response.personaId;
                infoVentaAux.personaId = personaIdAux;
                //Guardamos el domicilio
                response = guardaDomicilioTPlay(domicilio);
                if (response.domicilioId > 0 && !response.EstadoOperacion.Status)
                {
                    domicilioIdAux = response.domicilioId;
                    infoVentaAux.idDomicilioI = domicilioIdAux;
                    //Guardamos la relacion de venta
                    response = guardaVentaTPlay(infoVentaAux);
                    if (!response.EstadoOperacion.Status)
                    {
                        response = new EntResponseTPlay { EstadoOperacion = new EstadoOperacion(), personaId = personaIdAux, domicilioId = domicilioIdAux };
                    }
                    else
                    {
                        response = new EntResponseTPlay { EstadoOperacion = response.EstadoOperacion, personaId = personaIdAux, domicilioId = domicilioIdAux };
                    }//end if 3
                }
                else
                {
                    response = new EntResponseTPlay { EstadoOperacion = response.EstadoOperacion, personaId = personaIdAux, domicilioId = domicilioIdAux };
                }//end if 2
            }
            else
            {
                response = new EntResponseTPlay { EstadoOperacion = response.EstadoOperacion, personaId = personaIdAux, domicilioId = domicilioIdAux };
            }//end if 1
            return response;
        }

        public EntResponseInfoTPlay consultaInfoPedido(int pedido) {
            return new CtrlCombosTotalPlay().obtenerInfoVtaTPlay(pedido);
        }

        //Metodo para burlar certificado
        public void TrustAllCert()
        {
            System.Net.ServicePointManager.ServerCertificateValidationCallback =
            ((remitente, certificado, cadena, sslPolicyErrors) => true);
        }

        private string obtenerTokenCifradoCentral(string token)
        {
            string oEntRespuesta = string.Empty;
            try
            {
                CtrlCombosTotalPlay ctrlCombos = new CtrlCombosTotalPlay();
                string keyCifrado = ctrlCombos.RecuperaCatalogoGenerico(1744, 5).Trim();
                if (keyCifrado.Length != 0)
                {
                    string key = keyCifrado;
                    string vector = keyCifrado;
                    Elektra.Negocio.Prepago.Mediador.ManejadorCifradoAES manejadorCifrado = new Elektra.Negocio.Prepago.Mediador.ManejadorCifradoAES(key, vector);

                    oEntRespuesta = manejadorCifrado.EncryptText(token);

                }
            }
            catch (Exception e) 
            {
                oEntRespuesta = e.Message;
            }

            return oEntRespuesta;
        }


        public string validarCoberturaTP(string latitud, string longitud)
        {
            string oEntRespuesta = string.Empty;
            try
            {
                this.TrustAllCert();
                CtrlConsultaWebService consWS = new CtrlConsultaWebService();
                CtrlCombosTotalPlay ctrlCombos = new CtrlCombosTotalPlay();
                string cadena = string.Empty;
                string respuestaWS = string.Empty;
                string URL = ctrlCombos.RecuperaCatalogoGenerico(1744, 1).Trim();
                string usuario = ctrlCombos.RecuperaCatalogoGenerico(1744, 2).Trim();
                string contrasenia = ctrlCombos.RecuperaCatalogoGenerico(1744, 3).Trim();
                string usrCentral = ctrlCombos.RecuperaCatalogoGenerico(1744, 4).Trim();
                string usrCifrado = this.obtenerTokenCifradoCentral(usrCentral);
                string token = this.obtenerTokenCifradoCentral(latitud + "@" + longitud);

                if (URL.Length == 0)
                {
                    oEntRespuesta = "No se recuperó correctamente la URL de Cobertura, favor de contactar a soporte";
                }
                else
                    if (token.Length == 0 || usrCifrado.Length == 0)
                    {
                        oEntRespuesta = "No se recuperó correctamente el token de cifrado de Cobertura, favor de contactar a soporte";
                    }
                    else
                    {
                        string peticionWS = "{\"usuario\" : \"" + usrCifrado + "\",\"token\" : \"" + token + "\"," +
                                            "\"Ip\" : \"1.1.1.1\", \"Coordenadas\" : {\"latitud\" : \"" + latitud + "\",\"longitud\" : \"" + longitud + "\"," +
                                            "\"TipoCliente\" : \"Totalplay\"  }}";
                        respuestaWS = consWS.ConnectWS(URL, peticionWS, 60000, "Basic", usuario, contrasenia);

                        //oEntRespuesta.mensajeError = respuestaWS; //consWS.JScriptSerializa.Deserialize<EntRespuestaOrdenCatExt>(respuestaWS);


                        cadena = cadena + ", petición: " + peticionWS + ",respuestaWS: " + respuestaWS;
                        oEntRespuesta = respuestaWS;
                        log.Info("Validación de cobertura: " + cadena);
                    }
            }
            catch (Exception e)
            {
                oEntRespuesta = e.Message;
            }

            return oEntRespuesta;
        }
        public int ValidaTienePaquetes(int sku) {
            return new CtrlCombosTotalPlay().validaSiTienePaquetes(sku);
        }
        public EntRespuestaPaquetePromocion ObtenerProductosEnPaqueteApi(EntConsultaApiPaquete ConsultaApiPaquete)
        {
            EntRespuestaPaquetePromocion respuesta = new EntRespuestaPaquetePromocion();
            respuesta = new CtrlCombosTotalPlay().ObtenerProductosEnPaqueteApi(ConsultaApiPaquete.SKU);

            EntParametrosNegocio parame = new EntParametrosNegocio();
            parame.BeginObject(181);
            if (parame.Valor == "1")    
                RecuperaPPApi(respuesta,ConsultaApiPaquete);

            return respuesta;


            //return new CtrlCombosTotalPlay().ObtenerProductosEnPaqueteApi(sku,ConsultaApiPaquete);
            
        }

        public EntRespuestaPaquetePromocion RecuperaPPApi(EntRespuestaPaquetePromocion respuesta, EntConsultaApiPaquete ConsultaApiPaquete)
        {
            int totalPaquetes = 0;
            EngancheAplicadoProrateo = new Hashtable();
            ArrayList LstPromoPaqueteid = new ArrayList();
            ArrayList listaDetalleVenta = new ArrayList();
            EntNEw.EntCarritoRequestVenta objetoPeticion; 
            EntPaquetePromocion paquetesid=new EntPaquetePromocion();
            int canal = 0;
			int pais = 0;
			int sucursal = 0;

         try
			{

                if (respuesta.Paquetes != null && respuesta.Paquetes.Count > 0)
                {
                    for (int i = 0; i < respuesta.Paquetes.Count; i++)
                    {
                        if (!LstPromoPaqueteid.Contains(respuesta.Paquetes[i].PaqueteId))
                        {
                            LstPromoPaqueteid.Add(respuesta.Paquetes[i].PaqueteId);
                            totalPaquetes = totalPaquetes + 1;
                        }
                    }
                }

                //Consultar datos tienda
				ConsultarDatosTienda(ref canal, ref pais, ref sucursal);
	
          
                for (int i=0; i <totalPaquetes; i ++)
                {
                    listaDetalleVenta = new ArrayList();
                    EngancheAplicadoProrateo = new Hashtable();
                    
                    objetoPeticion = new EntNEw.EntCarritoRequestVenta();
                    
                    for (int j = 0; j < respuesta.Paquetes.Count; j++)
                        {
                            if (respuesta.Paquetes[j].AplicaCredito)
                            {
                                if (respuesta.Paquetes[j].PaqueteId == (int)LstPromoPaqueteid[i])
                                {
                                    int idpaq = (int)LstPromoPaqueteid[i];
                                    EntDetalleVentaBaseNST detalleVenta = new EntDetalleVentaBaseNST();
                                    detalleVenta.SKU = respuesta.Paquetes[j].SKU;
                                    detalleVenta.precioLista = respuesta.Paquetes[j].ProdPrecio;
                                    //Prorrateo Enganche
                                    detalleVenta.montoEnganche = AplicaEngancheProrrateo(respuesta, idpaq, respuesta.Paquetes[j].SKU, totalPaquetes, ConsultaApiPaquete.Enganche);
                                    detalleVenta.Cantidad = respuesta.Paquetes[j].Cantidad;
                                    detalleVenta.montoDescuento = respuesta.Paquetes[j].ProdDescuento;
                                    detalleVenta.plazos = new int[1];
                                    detalleVenta.plazos[0] = ConsultaApiPaquete.Plazo;


                                    objetoPeticion.lstEntDetalleVentaBaseNST.Add(detalleVenta);
                                }
                            }
                        }

                    if (objetoPeticion.lstEntDetalleVentaBaseNST.Count == 0)
                        continue;

                    objetoPeticion.canal = canal;
                    objetoPeticion.pais = pais;
                    objetoPeticion.tienda = sucursal;
                    objetoPeticion.tipoPeriodo = 1;


                   

                    if (ConsultaApiPaquete.ClienteNST.folioCU != 0)
                    {

                        if (ConsultaApiPaquete.ClienteNST.ResultadoApi != null)
                        {
                            if (ConsultaApiPaquete.TipoProd == 0)
                            {
                                for (int k = 0; k < ConsultaApiPaquete.ClienteNST.ResultadoApi.ProductosTipoCliente.Length; k++)
                                {
                                    if (ConsultaApiPaquete.ClienteNST.ResultadoApi.ProductosTipoCliente[k].IdProducto == 21 && ConsultaApiPaquete.TipoProd == EnumTipoProductoNST.mercancias)
                                    {
                                        objetoPeticion.tipoCliente = ConsultaApiPaquete.ClienteNST.ResultadoApi.ProductosTipoCliente[k].CodigoTipoTasa;
                                        objetoPeticion.tipoPromocion = ConsultaApiPaquete.ClienteNST.ResultadoApi.ProductosTipoCliente[k].CodigoPromocionTasa;
                                        break;
                                    }
                                    if (ConsultaApiPaquete.ClienteNST.ResultadoApi.ProductosTipoCliente[k].IdProducto == 22 && ConsultaApiPaquete.TipoProd == EnumTipoProductoNST.motos )
                                    {
                                        objetoPeticion.tipoCliente = ConsultaApiPaquete.ClienteNST.ResultadoApi.ProductosTipoCliente[k].CodigoTipoTasa;
                                        objetoPeticion.tipoPromocion = ConsultaApiPaquete.ClienteNST.ResultadoApi.ProductosTipoCliente[k].CodigoPromocionTasa;
                                        break;
                                    }
                                    if (ConsultaApiPaquete.ClienteNST.ResultadoApi.ProductosTipoCliente[k].IdProducto == 23 && ConsultaApiPaquete.TipoProd == EnumTipoProductoNST.telefonia)
                                    {
                                        objetoPeticion.tipoCliente = ConsultaApiPaquete.ClienteNST.ResultadoApi.ProductosTipoCliente[k].CodigoTipoTasa;
                                        objetoPeticion.tipoPromocion = ConsultaApiPaquete.ClienteNST.ResultadoApi.ProductosTipoCliente[k].CodigoPromocionTasa;
                                        break;
                                    }
                                }
                            }

                        }
                        else
                        {

                            throw new ApplicationException("No se consulto la informacion de ResultadoApi");
                        }
                    }
                    else
                    {
                        if (ConsultaApiPaquete.TipoProd == EnumTipoProductoNST.mercancias)
                        {
                            objetoPeticion.tipoCliente = "56";
                            objetoPeticion.tipoPromocion = "21";
                        }

                        if (ConsultaApiPaquete.TipoProd == EnumTipoProductoNST.motos)
                        {
                            objetoPeticion.tipoCliente = "56";
                            objetoPeticion.tipoPromocion = "36";
                        }
                        if (ConsultaApiPaquete.TipoProd == EnumTipoProductoNST.telefonia)
                        {
                            objetoPeticion.tipoCliente = "3";
                            objetoPeticion.tipoPromocion = "0";
                        }
                    }

                    //Consulta Api
                    EntPlazosVenta objetoAbonosAPI = new ApiC.ManejadorAPISCredito().APICotizarVentaCredito(objetoPeticion);

                     if (objetoAbonosAPI != null && objetoAbonosAPI.oEntRespuestaNST.eTipoError != EnumTipoErrorNST.SinError)
				    {
					    string cadenaError = string.Format("El servicio de consulta de API devuelve: {0}", objetoAbonosAPI.oEntRespuestaNST.mensajeError);
					    System.Diagnostics.Trace.WriteLine(cadenaError, "LOG");
					    throw new ApplicationException(cadenaError);
				    }

                     AgregaPPVentanaPaquetes(respuesta, objetoAbonosAPI, (int) LstPromoPaqueteid[i]);
                    
                }
			}
			catch(Exception ex)
			{
                System.Diagnostics.Trace.WriteLine("StackTrace: " + ex.StackTrace, "LOG");
                System.Diagnostics.Trace.WriteLine("Message: " + ex.Message, "LOG");
                System.Diagnostics.Trace.WriteLine("Falla en RecuperaPPApi()", "LOG");
                throw new ApplicationException(ex.Message);
			}

            return respuesta;
        }

        private void AgregaPPVentanaPaquetes(EntRespuestaPaquetePromocion respuesta, EntPlazosVenta objetoAbonosAPI, int idPromo)
        {
            try
            {
                System.Diagnostics.Trace.WriteLine("Inicia metodo AgregaPPVentanaPaquetes()", "LOG");

                foreach (EntPaquetePromocion detallePaquetes in respuesta.Paquetes)
                {
                    if (detallePaquetes.IdPromocion != idPromo)
                        continue;
                    foreach (EntPlazosSKU plazoProducto in objetoAbonosAPI.LstPlazosSKU)
                    {
                        if (plazoProducto.SKU != (int)detallePaquetes.SKU)
                            continue;

                        foreach (EntPlazoAbono plazoApi in plazoProducto.lstEntPlazoAbono)
                        {
                            detallePaquetes.PagoPuntual = plazoApi.abonoPuntual;
                        }

                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine("StackTrace: " + ex.StackTrace, "LOG");
                System.Diagnostics.Trace.WriteLine("Message: " + ex.Message, "LOG");
                System.Diagnostics.Trace.WriteLine("Falla en AgregaPPVentanaPaquetes()", "LOG");
                throw new ApplicationException(ex.Message);
            }
        }



        private void ConsultarDatosTienda(ref int p_Canal, ref int p_Pais, ref int p_Tienda)
        {
            EntTienda consulta = new EntTienda();
            DataSet dSet = new DataSet();

            System.Diagnostics.Trace.WriteLine("Inicia metodo ConsultarDatosTienda()", "LOG");

            dSet = consulta.ConsultaDatosControl();

            if (!(dSet != null && dSet.Tables.Count > 0 && dSet.Tables[0].Rows.Count > 0))
                throw new ApplicationException("No fue posible obtener los datos de control de la sucursal.");

            DataRow dRow = dSet.Tables[0].Rows[0];
            if (!(dRow["fiIdCanal"] is DBNull))
                p_Canal = Convert.ToInt32(dRow["fiIdCanal"]);
            if (!(dRow["fiIdPais"] is DBNull))
                p_Pais = Convert.ToInt32(dRow["fiIdPais"]);
            if (!(dRow["fiNoTienda"] is DBNull))
                p_Tienda = Convert.ToInt32(dRow["fiNoTienda"]);
        }

        public decimal AplicaEngancheProrrateo(EntRespuestaPaquetePromocion respuesta, int Paqueteid, int skuEmisor, int totalPaquetes,decimal enganche)
        {
            decimal PorcEng = 0;
            int sumaTot = 0;
            ArrayList productosPaq = new ArrayList();

            if (!EngancheAplicadoProrateo.Contains(skuEmisor))
            {
                productosPaq = new ArrayList();
                sumaTot = 0;
                for (int j = 0; j < respuesta.Paquetes.Count; j++)
                {
                    if (respuesta.Paquetes[j].PaqueteId == Paqueteid)
                    {
                        productosPaq.Add(respuesta.Paquetes[j].SKU);
                        sumaTot = sumaTot + (int)respuesta.Paquetes[j].ProdPrecio;
                    }
                }


                for (int j = 0; j < respuesta.Paquetes.Count; j++)
                {
                    foreach (int productos in productosPaq)
                    {
                        if (respuesta.Paquetes[j].SKU == productos)
                        {
                            decimal PorcPaq = 0;
                            PorcPaq = Math.Round((respuesta.Paquetes[j].ProdPrecio - respuesta.Paquetes[j].ProdDescuento) * enganche);
                            if (!EngancheAplicadoProrateo.Contains(respuesta.Paquetes[j].SKU))
                            {
                                EngancheAplicadoProrateo.Add(respuesta.Paquetes[j].SKU, PorcPaq);
                            }
                        }
                    }
                }

                PorcEng = Convert.ToDecimal(EngancheAplicadoProrateo[skuEmisor].ToString());
            }
            else
            {
                PorcEng = Convert.ToDecimal(EngancheAplicadoProrateo[skuEmisor].ToString());

            }

            return PorcEng;
            
        }
        #endregion MetodosPublicos

        #region  promocionesBancarias
        public EntBancosParticipantes obtenerBancosParticipantes()
        {
            return new CtrlPromocionesBanco().obtenerBancosParticipantesBD();
        }
        public EntResValidaBanco validarBinBanco(int idBanco, string bin, int idPromocion)
        {
            return new CtrlPromocionesBanco().validarBinBancoDB(idBanco, bin, idPromocion);
        }
        public EntResValidaBanco validarPromocionBanco(int noPedido)
        {
            return new CtrlPromocionesBanco().validarPromocionBancoDB(noPedido);
        }
        #endregion promocionesBancarias
    }
}
