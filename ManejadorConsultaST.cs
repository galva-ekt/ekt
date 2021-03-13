using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ElektraNegocioEntidades.NewServicioTienda;
using Elektra.Negocio.Entidades.Producto;
using Elektra.Negocio.Entidades.Ventas;
using ElektraNegocioEntidades.PromocionesIPAD;
using Elektra.Negocio.Surtimiento.ImpresionIpad;
using Elektra.Negocio.PromocionesNewServicioTienda;
using Elektra.Negocio.Utilerias;
using Elektra.Negocio.ReglasGenericas;
using GuiasEmpleado.Negocio;
using GuiasEmpleado.Entidades;
using System.Data;
using Elektra.Negocio.Entidades.NewServicioTienda;
using Elektra.Negocio.Entidades.Tienda;
using Transportes = Elektra.Negocio.Comunicaciones.Transportes.Cotizador;
using EntTransportes = Elektra.Negocio.Entidades.Transportes;
using Elektra.Negocio.Entidades.Prepago;
using Elektra.Negocio.Prepago.Mediador;
using Elektra.Negocio.CatOmicanal;
using System.Data.SqlClient;
using System.Net;
using POS.Telefonia.Negocio.ServicioTienda;
using POS.Telefonia.Logs;
using System.IO;
using System.Web.Script.Serialization;
using Elektra.Negocio.PagosManejador;
using Elektra.Negocio.Entidades.PagosManejador;
using System.Collections;
/*using Elektra.Negocio.InventarioCentral.Services;
using ConsInv = Elektra.Negocio.InventarioCentral.DTO.Consulta;
using System.Collections;
using Elektra.Services.Transaction;*/
using Elektra.Negocio.MVNO;
using Elektra.Negocio.Entidades.VentaUnicaTelefonia;
using Newtonsoft.Json;
using System.Web.Script.Serialization;
using System.Xml;
using System.Xml.Serialization;
using System.Diagnostics;
using Elektra.Negocio.CombosTotalPlay;
using Elektra.Negocio.APICredito;

namespace Elektra.Negocio.NewServicioTienda
{
    public class ManejadorConsultaST
    {
        private bool esTelefonia = false, esSurtTelefonia = true, agregaDetalleBono = true, ContieneClienteContado = false;
        private DetalleVentaBonoNST[] lstDetalleVentaBonoAux;
        private EntSeguroIpad oEntSeguroIpad = new EntSeguroIpad();/// <summary>
        /// Cadenas que sirven para borrar la basura al serializar un objeto.
        /// </summary>
        private string[] oldString = { "<?xml version=\"1.0\" encoding=\"utf-8\"?>", "xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\"", "xmlns=\"http://schemas.datacontract.org/2004/07/Elektra.Negocio.Entidades.SegurosIpad\"", "xmlns = \"http://schemas.datacontract.org/2004/07/Elektra.Servicios.Gerente.Recompra\"" };

        private LogsTelefonia _logs = new LogsTelefonia { nombreLog = "ServicioTienda.log", rutaLogs = @"E:\ADN\NET64_EKT\Telefonia\PuntoDeVenta\Logs\", tamanioMaximoLog = 1, unidadTamanioMaximoLog = EnumTamanioMaximo.Mega };
        private bool bloqueoMileniaAPI = false;
        private string codigoRecomendador = string.Empty;
        private string tipoPromocion = string.Empty;

        #region Métodos Publicos
        public EntRespuestaCotizarVentasNST CotizarVentas(List<EntDetalleVentaBaseNST> lstEntDetalleVentaBaseNST, List<EntPromocionEspecialNST> lstEntPromocionEspecialNST, EntVentaEmpleadoNST oEntVentaEmpleadoNST, EntComplementosNST oEntComplementosNST)
        {
            var metodo = this.GetType().Name + "." + System.Reflection.MethodBase.GetCurrentMethod().Name;
            _logs.AppendLine(metodo, "Inicia");
            System.Diagnostics.Trace.WriteLine("Inicio de CotizarVentas", "LOG");

            WCFServicioTienda wsWCFServicioTienda = new WCFServicioTienda();
            RespuestaCotizarVentas oRespuestaCotizarVentas = new RespuestaCotizarVentas();
            EntRespuestaCotizarVentasNST oEntRespuestaCotizarVentasNST = new EntRespuestaCotizarVentasNST();
            EntVentaActualCotizar[] lstEntVentaActualCotizar;
            bool EsProdUOIFI = false;
            bool EsRecargaInicial = false;
            int skuRecargaInicial = 0;
            int skuRecargaLibre = 0;
            bool esTelefoniaLibre = false;

            try
            {
                System.Diagnostics.Trace.WriteLine("Inicio de CotizarVentas 1", "LOG");
                #region configRecargainicial(0)
                DataSet configRecargaInicial = new DataSet();
                configRecargaInicial = this.GetConfigRecargainicial(0);

                if (configRecargaInicial != null && configRecargaInicial.Tables[0] != null && configRecargaInicial.Tables.Count > 0 && configRecargaInicial.Tables[0].Rows.Count > 0)
                {
                    skuRecargaInicial = Convert.ToInt32(configRecargaInicial.Tables[0].Rows[0]["SkuRecargaInicial"]);   // 33001151
                    System.Diagnostics.Trace.WriteLine(metodo + " skuRecargaInicial: " + skuRecargaInicial.ToString(), "LOG");
                }
                #endregion
                #region skuRecargalibre
                EntCatalogos catalogo = new EntCatalogos();
                System.Diagnostics.Trace.WriteLine("Inicia  busuqeda de SKU para  recarga ", "log");
                DataSet dsCatalogo = catalogo.ObtenerCatalogoGenericoMaestro(1735, 12);
                if (dsCatalogo != null && dsCatalogo.Tables != null && dsCatalogo.Tables.Count > 0 && dsCatalogo.Tables[0].Rows.Count > 0)
                {
                    skuRecargaLibre = Convert.ToInt32(dsCatalogo.Tables[0].Rows[0]["fcCatDesc"].ToString());
                }
                System.Diagnostics.Trace.WriteLine("SKU de recarga  " + skuRecargaLibre, "log");
                System.Diagnostics.Trace.WriteLine("Inicio de CotizarVentas 2", "LOG");
                #endregion


                if (oEntComplementosNST.oAddressClient == null)
                {
                    for (int i = 0; i < lstEntDetalleVentaBaseNST.Count; i++)
                    {
                        if (CtrlReglasGenericas.AplicarRegla(829, lstEntDetalleVentaBaseNST[i].SKU))
                        {
                            EsProdUOIFI = true;
                            break;
                        }

                        // Recarga Inicial Telcel $100
                        if (lstEntDetalleVentaBaseNST[i].SKU == skuRecargaInicial)
                        {
                            EsRecargaInicial = true;
                            //break;
                        }
                        if (lstEntDetalleVentaBaseNST[i].SKU == skuRecargaLibre)
                        {
                            EsRecargaInicial = true;
                            //break;
                        }

                        if (CtrlReglasGenericas.AplicarRegla(519, lstEntDetalleVentaBaseNST[i].SKU))
                        {
                            esTelefoniaLibre = true;
                            //break;
                        }
                    }

                    System.Diagnostics.Trace.WriteLine("Existe un telefono de negocio libre   " + esTelefoniaLibre, "log");

                    if (EsProdUOIFI)
                    {
                        System.Diagnostics.Trace.WriteLine("Se intenta cotizar productos de OUIFI desde EPOS", "LOG");
                        oEntRespuestaCotizarVentasNST.oEntRespuestaNST.eTipoError = EnumTipoErrorNST.Error;
                        oEntRespuestaCotizarVentasNST.oEntRespuestaNST.mensajeError = "Estos productos no pueden ser vendidos por el cotizador de EPOS.";
                        _logs.EscribeLog();
                        return oEntRespuestaCotizarVentasNST;
                    }
                }

                System.Diagnostics.Trace.WriteLine("Inicio de CotizarVentas 3", "LOG");

                if (lstEntDetalleVentaBaseNST != null)
                {
                    for (int i = 0; i < lstEntDetalleVentaBaseNST.Count; i++)
                    {
                        if (CtrlReglasGenericas.AplicarRegla(677, lstEntDetalleVentaBaseNST[i].SKU))
                        {
                            if (!oEntVentaEmpleadoNST.DescCompania.Contains("Portal_OUI"))
                            {
                                System.Diagnostics.Trace.WriteLine("Se intenta cotizar sim card de OUI desde EPOS", "LOG");
                                oEntRespuestaCotizarVentasNST.oEntRespuestaNST.eTipoError = EnumTipoErrorNST.Error;
                                oEntRespuestaCotizarVentasNST.oEntRespuestaNST.mensajeError = "Estos productos no pueden ser vendidos por el cotizador de EPOS.";
                                _logs.EscribeLog();
                                return oEntRespuestaCotizarVentasNST;
                            }
                            break;
                        }


                        // Recarga Inicial Telcel $100
                        if (lstEntDetalleVentaBaseNST[i].SKU == skuRecargaInicial)
                        {
                            EsRecargaInicial = true;
                            //break;
                        }
                        if (lstEntDetalleVentaBaseNST[i].SKU == skuRecargaLibre)
                        {
                            EsRecargaInicial = true;
                        }
                    }
                }

                System.Diagnostics.Trace.WriteLine("Inicio de CotizarVentas 4", "LOG");

                if (EsRecargaInicial)
                {
                    System.Diagnostics.Trace.WriteLine("EsRecargaInicial en CONTADO: " + EsRecargaInicial.ToString(), "LOG");

					bool entroSKU = false;
                    for (int i = 0; i < lstEntDetalleVentaBaseNST.Count; i++)
                    {
                        if (lstEntDetalleVentaBaseNST[i].SKU == skuRecargaInicial)
                        {
                            if (lstEntDetalleVentaBaseNST[i].precioLista == 0)
                            {
                                entroSKU = true;
                            }
                            lstEntDetalleVentaBaseNST.Remove(lstEntDetalleVentaBaseNST[i]);
                        }
                        if (skuRecargaInicial != skuRecargaLibre)
                        {
                            if (lstEntDetalleVentaBaseNST[i].SKU == skuRecargaLibre)
                            {
                                if (lstEntDetalleVentaBaseNST[i].precioLista == 0)
                                {
                                    entroSKU = true;
                                }
                                lstEntDetalleVentaBaseNST.Remove(lstEntDetalleVentaBaseNST[i]);
                            }
                        }
                    }
                    for (int i = 0; i < oEntRespuestaCotizarVentasNST.lstEntDetalleVentaResNST.Count; i++)
                    {
                        if (oEntRespuestaCotizarVentasNST.lstEntDetalleVentaResNST[i].SKU == skuRecargaInicial)
                        {
                            oEntRespuestaCotizarVentasNST.lstEntDetalleVentaResNST.Remove(oEntRespuestaCotizarVentasNST.lstEntDetalleVentaResNST[i]);
                        }
                        if (skuRecargaInicial != skuRecargaLibre)
                        {
                            if (oEntRespuestaCotizarVentasNST.lstEntDetalleVentaResNST[i].SKU == skuRecargaLibre)
                            {
                                oEntRespuestaCotizarVentasNST.lstEntDetalleVentaResNST.Remove(oEntRespuestaCotizarVentasNST.lstEntDetalleVentaResNST[i]);
                            }
                        }
                    }

                    System.Diagnostics.Trace.WriteLine("Se intenta cotizar Recarga Inicial en Venta de Contado", "LOG");
                    oEntRespuestaCotizarVentasNST.oEntRespuestaNST.eTipoError = EnumTipoErrorNST.Error;
                    oEntRespuestaCotizarVentasNST.oEntRespuestaNST.mensajeError = "Estos productos no pueden ser vendidos por el cotizador de EPOS en una venta de contado.";
                    _logs.EscribeLog();
                    if (entroSKU)
                    {
                        return oEntRespuestaCotizarVentasNST;
                    }
                }

                System.Diagnostics.Trace.WriteLine("Inicio de CotizarVentas 5", "LOG");

                System.Diagnostics.Trace.WriteLine("CotizarVentas()", "LOG");
                new ManejadorPromocionesNST().LimpiarPromociones(lstEntDetalleVentaBaseNST);
                System.Diagnostics.Trace.WriteLine("Inicio de CotizarVentas 6", "LOG");
                this.lstDetalleVentaBonoAux = this.CrearDetalleVentaBono(lstEntDetalleVentaBaseNST, false);
                System.Diagnostics.Trace.WriteLine("Inicio de CotizarVentas 7", "LOG");
                this.CrearDetalleSeguroVidaMax(lstEntDetalleVentaBaseNST, 0, false);
                System.Diagnostics.Trace.WriteLine("Inicio de CotizarVentas 8", "LOG");
                ClienteIpadBase oClienteIpadBase = this.CrearClienteIpadBase(null, null, null);
                System.Diagnostics.Trace.WriteLine("Inicio de CotizarVentas 9", "LOG");
                oEntRespuestaCotizarVentasNST.oEntRespuestaNST = new ManejadorVentaNST().ValidarConvivenciaProductos(lstEntDetalleVentaBaseNST);
                System.Diagnostics.Trace.WriteLine("Inicio de CotizarVentas 10", "LOG");
                if (lstEntPromocionEspecialNST != null)
                    new ManejadorPromocionesEspecialesNST().AgregarPromocionesEspeciales(lstEntPromocionEspecialNST, lstEntDetalleVentaBaseNST, EnumTipoVentaNST.Contado);
                lstEntVentaActualCotizar = this.CreaLstEntVentaActualCotizar(lstEntDetalleVentaBaseNST, oEntComplementosNST);
                InformacionEmpleado oInformacionEmpleado = this.CrearInformacionEmpleado(oEntVentaEmpleadoNST);
                this.ValidarVentaApartado(lstEntVentaActualCotizar, lstEntPromocionEspecialNST);
                wsWCFServicioTienda.Timeout = 600000;
                oRespuestaCotizarVentas = wsWCFServicioTienda.CotizarVentas(lstEntVentaActualCotizar, oClienteIpadBase, this.oEntSeguroIpad, new Atributo[0], "", "", "", oInformacionEmpleado);
                this.AgregarOmitirPromociones(ref oRespuestaCotizarVentas, lstEntVentaActualCotizar);
                oEntRespuestaCotizarVentasNST = this.CreaRespuestaCotizarVentas(oRespuestaCotizarVentas, oEntRespuestaCotizarVentasNST.oEntRespuestaNST, oEntComplementosNST);
                this.ModificaTipoAgregado(lstEntDetalleVentaBaseNST, oEntRespuestaCotizarVentasNST.lstEntDetalleVentaResNST);
                oEntRespuestaCotizarVentasNST.totalVentaComplementos = this.CalculaTotalComplementos(oEntRespuestaCotizarVentasNST.lstEntDetalleVentaResNST);
                this.GenerarVentaApartado(oRespuestaCotizarVentas, lstEntPromocionEspecialNST);

                //if (this.lstDetalleVentaBonoAux.Length > 0)
                //    this.ActualizaProductosBono(oRespuestaCotizarVentas.ListaVentaActual[0].ListaDetalleVenta, oEntRespuestaCotizarVentasNST.lstEntDetalleVentaResNST);

                oEntRespuestaCotizarVentasNST.lstEntPromocionEspecialNST = lstEntPromocionEspecialNST;
                this.ActulizaPromEspecial(oEntRespuestaCotizarVentasNST);
                oEntRespuestaCotizarVentasNST.oEntVentaEmpleadoNST = oEntVentaEmpleadoNST;
                oEntRespuestaCotizarVentasNST.totalVentaBonos = this.ActualizaTotalBono(oEntRespuestaCotizarVentasNST.lstEntDetalleVentaResNST);
                this.ConsultarDescuentosCredito(lstEntDetalleVentaBaseNST, oEntRespuestaCotizarVentasNST.lstEntDetalleVentaResNST, oEntRespuestaCotizarVentasNST.lstEntPromocionEspecialNST);

                for (int act = 0; act < oEntRespuestaCotizarVentasNST.lstEntDetalleVentaResNST.Count; act++)
                {
                    if (CtrlReglasGenericas.AplicarRegla(7, oEntRespuestaCotizarVentasNST.lstEntDetalleVentaResNST[act].SKU) == true)
                    {
                        oEntRespuestaCotizarVentasNST.lstEntDetalleVentaResNST[act].accionItk = oRespuestaCotizarVentas.ListaVentaActual[0].ListaDetalleVenta[act].accionItk;
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                oEntRespuestaCotizarVentasNST.oEntRespuestaNST.eTipoError = EnumTipoErrorNST.Error;
                oEntRespuestaCotizarVentasNST.oEntRespuestaNST.mensajeError = ex.Message;
            }

            return oEntRespuestaCotizarVentasNST;
        }

        private void ActulizaPromEspecial(EntRespuestaCotizarVentasNST oEntRespuestaCotizarVentasNST)
        {
            foreach (EntPromocionEspecialNST promesp in oEntRespuestaCotizarVentasNST.lstEntPromocionEspecialNST)
            {
                bool aplicapromo = false;
                if (promesp.eTipoPromocion == EnumTipoPromocionNST.DescuentoCliente && promesp.eTipoFolioSolicitadoNST == EnumTipoFolioSolicitadoNST.Folio)
                {
                    foreach (EntDetalleVentaResNST detalleres in oEntRespuestaCotizarVentasNST.lstEntDetalleVentaResNST)
                    {
                        foreach (EntPromocionAplicadaNST promapli in detalleres.lstEntPromocionAplicadaNST)
                        {
                            if (((promapli.promocionId == promesp.promocionId) && (promapli.montoOtorgado > 0)))
                            {
                                aplicapromo = true;
                                break;
                            }
                        }
                        if (aplicapromo)
                            break;
                    }

                    if (!aplicapromo)
                    {
                        promesp.montoOtorgado = 0;
                        promesp.porcentajeOtorgado = 0;
                    }
                }
            }
        }

        public string ConsultaParametroNegocio(int parametro)
        {
            string Valor = string.Empty;
            try
            {
                EntConsultasBDNST consulta = new EntConsultasBDNST();
                DataSet dsCampos = consulta.ObtenerParametroNegocio(parametro);

                if (dsCampos != null && dsCampos.Tables != null && dsCampos.Tables.Count > 0 && dsCampos.Tables[0].Rows.Count > 0)
                {
                    Valor = dsCampos.Tables[0].Rows[0]["fvValor"].ToString().Trim();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine("Error al consultar el parametro " + parametro.ToString() + ". Msj: " + ex.Message + ". Trace: " + ex.StackTrace, "LOG");
                return string.Empty;
            }
            return Valor;
        }

        public bool ConsultaOcultarServicios()
        {
            bool ocultarVentana = false;

            DataSet dsParametro = new EntCatalogos().ObtenerDatosParametro(6102);
            if (dsParametro != null && dsParametro.Tables.Count > 0 && dsParametro.Tables[0] != null && dsParametro.Tables[0].Rows.Count > 0
                && dsParametro.Tables[0].Columns != null && dsParametro.Tables[0].Columns.Count > 0)
            {
                ocultarVentana = Convert.ToInt32(dsParametro.Tables[0].Rows[0]["fcPrmVal"]) == 1 ? true : false;
            }

            return ocultarVentana;
        }

        /// <summary>
        /// Indica si se bloquea  Milenia  cuando  entra un descuento en el  abono o si esta activo el parametro desde BD 6102
        /// </summary>
        /// <param name="oEntRespuestaCotizarVentasCredNST"></param>
        /// <returns></returns>
        public bool BloqueoMileaDesc(EntRespuestaCotizarVentasCredNST oEntRespuestaCotizarVentasCredNST)
        {
            var metodo = this.GetType().Name + "." + System.Reflection.MethodBase.GetCurrentMethod().Name;
            _logs.AppendLine(metodo, "Inicia");
            _logs.AppendLineJson(metodo, " oEntRespuestaCotizarVentasCredNST   ", new { oEntRespuestaCotizarVentasCredNST = oEntRespuestaCotizarVentasCredNST });
            bool bloqueoMilenia = false;

            bool parametroServicios = false;
            _logs.AppendLine(metodo, "Consulta Parametro BD de Bloqueo Milenia ");
            parametroServicios = this.ConsultaOcultarServicios();
            _logs.AppendLine(metodo, "Consulta Parametro BD de Bloqueo Milenia Resultado : " + parametroServicios);
            bool aplicaMileniaRenovaciones = false;

            EntCatalogos catalogo = new EntCatalogos();
            DataSet ds = catalogo.ObtenerCatalogoGenericoMaestro(1735);
            if (ds != null && ds.Tables != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
            {
                if (ds.Tables[0].Rows.Count > 7)
                    aplicaMileniaRenovaciones = Convert.ToInt32(ds.Tables[0].Rows[7]["fiSubItemStat"].ToString()) == 1 ? true : false;
            }
            _logs.AppendLine(metodo, "aplica Milenia para Renovaciones " + aplicaMileniaRenovaciones);


            _logs.AppendLine(metodo, "Aplica Cupon para promo 2X1: " + oEntRespuestaCotizarVentasCredNST.aplicaPromocionBF);
            _logs.AppendLine(metodo, "Mesaje de otras opciones : " + oEntRespuestaCotizarVentasCredNST.msjPromocionTel);
            _logs.AppendLine(metodo, "Aplica Descuento de Renovaciones : " + oEntRespuestaCotizarVentasCredNST.aplicaRenovacionTele);
            _logs.AppendLine(metodo, "Aplica Descuento en el Abono : " + oEntRespuestaCotizarVentasCredNST.aplicaDescuentoAbono);
            _logs.AppendLine(metodo, "Tipo de promocion Aplicada  " + tipoPromocion);
            if (parametroServicios || (oEntRespuestaCotizarVentasCredNST.aplicaPromocionBF && oEntRespuestaCotizarVentasCredNST.msjPromocionTel != null) || (!aplicaMileniaRenovaciones &&
                (oEntRespuestaCotizarVentasCredNST.aplicaRenovacionTele || oEntRespuestaCotizarVentasCredNST.aplicaDescuentoAbono)) || tipoPromocion == "2x1" ||
                (tipoPromocion == "Renovaciones" && !aplicaMileniaRenovaciones))
            {
                bloqueoMilenia = true;
            }
            _logs.AppendLine(metodo, " Se aplicara bloqueo en la milenia " + bloqueoMilenia);
            _logs.AppendLine(metodo, "Termina");
            _logs.EscribeLog();
            return bloqueoMilenia;

        }

        public ConsultaPagoTAZ ConsultaPagoTAZMSI(int Presupuesto)
        {
            ConsultaPagoTAZ taz = new ConsultaPagoTAZ();
            try
            {
                EntConsultasBDNST BDCon = new EntConsultasBDNST();
                DataSet dsInfo = BDCon.ConsultaBinesMSI(Presupuesto);

                if (dsInfo != null && dsInfo.Tables != null && dsInfo.Tables.Count > 0 && dsInfo.Tables[0].Rows.Count > 0)
                {
                    for (int i = 0; i < dsInfo.Tables[0].Rows.Count; i++)
                    {
                        taz.BinesValidos.Add(int.Parse(dsInfo.Tables[0].Rows[i]["fcBines"].ToString()));
                        taz.montoPagar = decimal.Parse(dsInfo.Tables[0].Rows[i]["fnmontoPagar"].ToString());
                        taz.montoTotal = decimal.Parse(dsInfo.Tables[0].Rows[i]["fnmontoTotal"].ToString());
                        taz.presupuestoId = int.Parse(dsInfo.Tables[0].Rows[i]["fipresupuestoId"].ToString());
                    }
                }
                else
                {
                    taz = new ConsultaPagoTAZ();
                    taz.oEntRespuestaNST.eTipoError = EnumTipoErrorNST.Warning;
                    taz.oEntRespuestaNST.mensajeError = "No se recuperaron valores para el presupuesto indicado.";
                }
            }
            catch (Exception ex)
            {
                taz = new ConsultaPagoTAZ();
                taz.oEntRespuestaNST.eTipoError = EnumTipoErrorNST.Error;
                taz.oEntRespuestaNST.mensajeError = ex.Message;
            }
            return taz;
        }

        public EntRespuestaCotizarVentasNST CotizarVentasCadena(string cadenaSkus)
        {
            EntRespuestaCotizarVentasNST oEntRespuestaCotizarVentasNST = new EntRespuestaCotizarVentasNST();
            List<EntDetalleVentaBaseNST> lstEntDetalleVentaBaseNST = new List<EntDetalleVentaBaseNST>();
            try
            {
                lstEntDetalleVentaBaseNST = this.CreaLstEntDetalleVentaBaseNST(cadenaSkus);
                if (lstEntDetalleVentaBaseNST.Count > 0)
                    oEntRespuestaCotizarVentasNST = this.CotizarVentas(lstEntDetalleVentaBaseNST, new List<EntPromocionEspecialNST>(), null, null);
                else
                {
                    oEntRespuestaCotizarVentasNST.oEntRespuestaNST.eTipoError = EnumTipoErrorNST.Warning;
                    oEntRespuestaCotizarVentasNST.oEntRespuestaNST.mensajeError = "No hay productos para cotizar";
                }
            }
            catch (Exception ex)
            {
                oEntRespuestaCotizarVentasNST.oEntRespuestaNST.eTipoError = EnumTipoErrorNST.Error;
                oEntRespuestaCotizarVentasNST.oEntRespuestaNST.mensajeError = ex.Message;
            }
            return oEntRespuestaCotizarVentasNST;
        }

        public List<EntContenidoPromocion> ConslultaPromocionBonoDescuento(string LstSKU, int TipoVenta)
        {
            EntConsultasBDNST BD = new EntConsultasBDNST();
            List<EntContenidoPromocion> lstProm = new List<EntContenidoPromocion>();
            DataSet dsRes = BD.ObtenerPromocionBonoDescuento(LstSKU);
            if (dsRes != null && dsRes.Tables != null && dsRes.Tables.Count > 0 && dsRes.Tables[0].Rows.Count > 0)
            {
                for (int i = 0; i < dsRes.Tables[0].Rows.Count; i++)
                {
                    if (Convert.ToInt32(dsRes.Tables[0].Rows[i]["fiTipoVentaId"].ToString()) == TipoVenta)
                    {
                        EntContenidoPromocion prom = new EntContenidoPromocion();
                        prom.idPromocion = Convert.ToInt32(dsRes.Tables[0].Rows[i]["fiPromocionId"].ToString());
                        prom.TipoPromocion = Convert.ToInt32(dsRes.Tables[0].Rows[i]["fiTipoPromocionId"].ToString());
                        prom.DescPromocion = dsRes.Tables[0].Rows[i]["fcDescripcion"].ToString();
                        lstProm.Add(prom);
                    }
                }
            }
            return lstProm;
        }

        private void RecalculaEnganche(ref EntEngancheNST oEntEngancheNST, RespuestaCotizarVentas oRespuestaCotizarVentas)
        {
            decimal MontoEnganche = 0;

            for (int i = 0; i < oRespuestaCotizarVentas.ListaVentaActual[0].ListaDetalleVenta.Length; i++)
            {
                MontoEnganche = MontoEnganche + (oRespuestaCotizarVentas.ListaVentaActual[0].ListaDetalleVenta[i].MontoEnganche * oRespuestaCotizarVentas.ListaVentaActual[0].ListaDetalleVenta[i].Cantidad);
            }
            oEntEngancheNST.montoFinanciar = Convert.ToDecimal(oRespuestaCotizarVentas.ListaVentaActual[0].MontoFinanciarEnganche);

            if (oEntEngancheNST.montoFinanciar > 0 && MontoEnganche > 0)
            {
                oEntEngancheNST.montoEnganche = MontoEnganche;
                oEntEngancheNST.porcentajeEnganche = Math.Round(((oEntEngancheNST.montoEnganche * 100) / oEntEngancheNST.montoFinanciar), 2);
            }

        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="edoId"></param>
        /// <param name="pobId"></param>
        /// <param name="isFiltro"></param>
        /// <returns></returns>
        public EntLstColoniasNST ConCP_ColoniasRefacciones(string edoId, string pobId, string isFiltro)
        {
            EntLstColoniasNST _entColonias = new EntLstColoniasNST();

            try
            {
                DataSet dscolonias = new EntTransportes.EntEnvioRefacciones().ObtieneColonias(Convert.ToInt32(edoId), Convert.ToInt32(pobId), Convert.ToInt32(isFiltro));

                if (dscolonias != null && dscolonias.Tables != null && dscolonias.Tables.Count > 0 && dscolonias.Tables[0].Rows.Count > 0)
                {
                    _entColonias.CP_Colonias = (from DataRow row in dscolonias.Tables[0].Rows
                                                select new EntColoniaRef
                                                {
                                                    fiPaisId = row["fiPaisId"].ToString(),
                                                    fiEdoId = row["fiEdoId"].ToString(),
                                                    fiPobId = row["fiPobId"].ToString(),
                                                    fcCteCP = row["fcCteCP"].ToString(),
                                                    fcColonia = row["fcColonia"].ToString()
                                                }).ToList();
                }
                else
                {
                    ApplicationException ApEx = new ApplicationException("No fue posible recuperar la información del CP y colonias.");
                    throw ApEx;
                }
            }
            catch (Exception ex)
            {
                _entColonias.CP_Colonias = new List<EntColoniaRef>();
                _entColonias.oEntRespuestaNST.eTipoError = EnumTipoErrorNST.Error;
                _entColonias.oEntRespuestaNST.mensajeError = ex.Message;
                System.Diagnostics.Trace.WriteLine("Error al consultar el CP y colonias para refacciones. " + " Mensaje: " + ex.Message + " Trace: " + ex.StackTrace, "LOG");
            }
            return _entColonias;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public EntCatalogosRefaccionesNST ConCatalogosRefacciones()
        {
            EntCatalogosRefaccionesNST _entCat = new EntCatalogosRefaccionesNST();

            try
            {
                DataSet dsCat = new EntTransportes.EntEnvioRefacciones().ObtieneCatalogos();

                if (dsCat != null && dsCat.Tables != null && dsCat.Tables.Count == 3 && dsCat.Tables[0].Rows.Count > 0 && dsCat.Tables[1].Rows.Count > 0 && dsCat.Tables[2].Rows.Count > 0)
                {
                    _entCat.Poblaciones = (from DataRow row in dsCat.Tables[0].Rows
                                           select new EntPoblacionRef
                                           {
                                               fiPobId = row["fiPobId"].ToString(),
                                               fiEdoId = row["fiEdoId"].ToString(),
                                               fiPaisId = row["fiPaisId"].ToString(),
                                               fcPobDesc = row["fcPobDesc"].ToString()
                                           }).ToList();

                    _entCat.Estados = (from DataRow row in dsCat.Tables[1].Rows
                                       select new EntEstadoRef
                                       {
                                           fiEdoId = row["fiEdoId"].ToString(),
                                           fiPaisId = row["fiPaisId"].ToString(),
                                           fcEdoDesc = row["fcEdoDesc"].ToString()
                                       }).ToList();

                    _entCat.MediosContacto = (from DataRow row in dsCat.Tables[2].Rows
                                              select new EntMedioContactoRef
                                              {
                                                  fiMedioContactoId = row["fiMedioContactoId"].ToString(),
                                                  fcMedioContactoDesc = row["fcMedioContactoDesc"].ToString()
                                              }).ToList();
                }
                else
                {
                    ApplicationException ApEx = new ApplicationException("No fue posible recuperar la información de los catalogos.");
                    throw ApEx;
                }
            }
            catch (Exception ex)
            {
                _entCat.Poblaciones = new List<EntPoblacionRef>();
                _entCat.Estados = new List<EntEstadoRef>();
                _entCat.MediosContacto = new List<EntMedioContactoRef>();
                _entCat.oEntRespuestaNST.eTipoError = EnumTipoErrorNST.Error;
                _entCat.oEntRespuestaNST.mensajeError = ex.Message;
                System.Diagnostics.Trace.WriteLine("Error al consultar los catalogos para refacciones. " + " Mensaje: " + ex.Message + " Trace: " + ex.StackTrace, "LOG");
            }
            return _entCat;
        }

        /// <summary>
        /// Consulta el detalle de compra para un folio de Refacciones
        /// </summary>
        /// <param name="Folio">Folio compra Refacciones</param>
        /// <returns>Detalle de la compra</returns>
        public EntInfoVentaRefacciones ConsultaFolioRefacciones(string Folio)
        {
            EntInfoVentaRefacciones respRefacciones = new EntInfoVentaRefacciones();
            try
            {
                Transportes.ComItalika _ComItalika = new Transportes.ComItalika();
                Transportes.InfoVentaRefacciones RespWSRefacciones = new Transportes.InfoVentaRefacciones();

                RespWSRefacciones = _ComItalika.ObtenerInformacionFolioItalika(Folio);

                if (RespWSRefacciones != null && RespWSRefacciones.IdError != 0 && RespWSRefacciones.MsnError.Trim().Length > 0)
                    throw new ApplicationException(RespWSRefacciones.MsnError.Trim());

                respRefacciones.MontoTotal = RespWSRefacciones.MontoTotal;
                respRefacciones.DescuentoTotal = RespWSRefacciones.DescuentoTotal;
                respRefacciones.CodigoGenerico = RespWSRefacciones.CodigoGenerico;
                respRefacciones.Folio = RespWSRefacciones.Folio;
                respRefacciones.MontoTotalIVA = RespWSRefacciones.MontoTotalIVA;
                respRefacciones.CodigoGenerico2 = RespWSRefacciones.CodigoGenerico2;

                if (RespWSRefacciones.DetalleVenta != null && RespWSRefacciones.DetalleVenta.Length > 0)
                {
                    foreach (Transportes.clsElementoVenta ElementoVta in RespWSRefacciones.DetalleVenta)
                    {
                        EntElementoVenta elemento = new EntElementoVenta();
                        elemento.Material = ElementoVta.Material;
                        elemento.CantidadSolicitada = ElementoVta.CantidadSolicitada;
                        elemento.CantidadConfirmada = ElementoVta.CantidadConfirmada;
                        elemento.Descripcion = ElementoVta.Descripcion;
                        elemento.Monto = ElementoVta.Monto;
                        elemento.Descuento = ElementoVta.Descuento;
                        respRefacciones.DetalleVenta.Add(elemento);
                    }
                }

            }
            catch (Exception ex)
            {
                respRefacciones = new EntInfoVentaRefacciones();
                respRefacciones.Folio = Folio;
                respRefacciones.oEntRespuestaNST.eTipoError = EnumTipoErrorNST.Error;
                respRefacciones.oEntRespuestaNST.mensajeError = ex.Message;
                System.Diagnostics.Trace.WriteLine("Error al consultar Folio de Refaccines. Folio: " + Folio.ToString() + " Mensaje: " + ex.Message + " Trace: " + ex.StackTrace, "LOG");
            }

            return respRefacciones;
        }

        public EntPlazosVenta ConsultaPlazosCalculadosVenta(string cadenaSkus, int PromocionCte, int TipoCte, int Periodo, int plazoMax, int PlazoMin, string Clasificacion, string PlazosValidos, bool AgregarPlazos)
        {
            EntPlazosVenta Lstplazos = new EntPlazosVenta();
            try
            {
                if (cadenaSkus.Trim().Length == 0)
                {
                    Lstplazos.oEntRespuestaNST.eTipoError = EnumTipoErrorNST.Error;
                    Lstplazos.oEntRespuestaNST.mensajeError = "Favor de enviar una cadena de sku para poder calcular los abonos.";
                    return Lstplazos;
                }


                EntConsultasBDNST conBD = new EntConsultasBDNST();
                DataSet dsPlazos = conBD.ObtenerPlazosCalculadosVenta(cadenaSkus, PromocionCte, TipoCte, Periodo, plazoMax, PlazoMin, Clasificacion, PlazosValidos, AgregarPlazos);

                if (dsPlazos != null && dsPlazos.Tables != null && dsPlazos.Tables.Count > 0 && dsPlazos.Tables[0].Rows.Count > 0)
                {
                    EntPlazosSKU plazosku = new EntPlazosSKU();
                    for (int i = 0; i < dsPlazos.Tables[0].Rows.Count; i++)
                    {

                        if (i == 0)
                            plazosku.SKU = int.Parse(dsPlazos.Tables[0].Rows[i]["fiProdid"].ToString());

                        if (plazosku.SKU == int.Parse(dsPlazos.Tables[0].Rows[i]["fiProdid"].ToString()))
                        {
                            EntPlazoAbono plazo = new EntPlazoAbono();
                            plazo.plazo = int.Parse(dsPlazos.Tables[0].Rows[i]["fiPlazo"].ToString());
                            plazo.abono = decimal.Parse(dsPlazos.Tables[0].Rows[i]["fnAbonoNormal"].ToString());
                            plazo.abonoPuntual = decimal.Parse(dsPlazos.Tables[0].Rows[i]["fnAbonoPuntual"].ToString());
                            plazo.ultimoAbono = decimal.Parse(dsPlazos.Tables[0].Rows[i]["fnUltimoAbono"].ToString());
                            plazo.porcentajeAbonoPuntual = decimal.Parse(dsPlazos.Tables[0].Rows[i]["fnDesctoTasaP"].ToString());
                            plazo.Periodo = int.Parse(dsPlazos.Tables[0].Rows[i]["fiPeriodo"].ToString());
                            plazo.Sobreprecio = decimal.Parse(dsPlazos.Tables[0].Rows[i]["fnSobreprecio"].ToString());
                            plazo.Mecanica = int.Parse(dsPlazos.Tables[0].Rows[i]["fiMecanica"].ToString());
                            plazosku.lstEntPlazoAbono.Add(plazo);
                        }
                        else
                        {
                            Lstplazos.LstPlazosSKU.Add(plazosku);
                            plazosku = new EntPlazosSKU();
                            plazosku.SKU = int.Parse(dsPlazos.Tables[0].Rows[i]["fiProdid"].ToString());
                            i--;
                        }

                    }
                    if (plazosku.lstEntPlazoAbono.Count > 0)
                        Lstplazos.LstPlazosSKU.Add(plazosku);
                }

            }
            catch (Exception ex)
            {
                Lstplazos = new EntPlazosVenta();
                Lstplazos.oEntRespuestaNST.eTipoError = EnumTipoErrorNST.Error;
                Lstplazos.oEntRespuestaNST.mensajeError = "Error al consultar los plazos calculados.";
                System.Diagnostics.Trace.WriteLine("Error al consultar los plazos calculados. Mensaje: " + ex.Message + " Trace: " + ex.StackTrace, "LOG");
            }
            return Lstplazos;
        }

        /// <summary>
        /// Busca Palzos de Promocion Credito 26 % 
        /// </summary>
        /// <param name="clasificacion"></param>
        /// <param name="plazMin"></param>
        /// <param name="plaMax"></param>
        /// <param name="fecha"></param>
        /// <param name="cadena"></param>
        /// <param name="tipoCalculo"></param>
        /// <param name="periodo"></param>
        /// <returns></returns>
        public EntPlazosVenta ConsultaPlazosPromo(string clasificacion, int plazMin, int plaMax, string fecha, string cadena, int tipoCalculo, int periodo)
        {
            var metodo = this.GetType().Name + "." + System.Reflection.MethodBase.GetCurrentMethod().Name;
            _logs.AppendLine(metodo, "Inicia");

            EntPlazosVenta Lstplazos = new EntPlazosVenta();
            try
            {
                if (cadena.Trim().Length == 0)
                {
                    Lstplazos.oEntRespuestaNST.eTipoError = EnumTipoErrorNST.Error;
                    Lstplazos.oEntRespuestaNST.mensajeError = "Favor de enviar una cadena de sku para poder calcular los abonos.";
                    return Lstplazos;
                }
                AbonosPromo abonosPromos = new AbonosPromo();
                Lstplazos = abonosPromos.Abonos(clasificacion, plazMin, plaMax, fecha, cadena, tipoCalculo, periodo);
                _logs.AppendLineJson(metodo, "Lista plazos: ", new { Lstplazos = Lstplazos });

            }
            catch (Exception ex)
            {
                Lstplazos = new EntPlazosVenta();
                Lstplazos.oEntRespuestaNST.eTipoError = EnumTipoErrorNST.Error;
                Lstplazos.oEntRespuestaNST.mensajeError = "Error al consultar los plazos calculados.";
                System.Diagnostics.Trace.WriteLine("Error al consultar los plazos calculados. Mensaje: " + ex.Message + " Trace: " + ex.StackTrace, "LOG");
                _logs.AppendLineError(metodo, "Error al consultar los plazos calculados. Mensaje: " + ex.Message + " Trace: " + ex.StackTrace);
                _logs.AppendLine(metodo, "Termina en error");
                _logs.EscribeLog();
            }
            _logs.AppendLine(metodo, "Termina");
            return Lstplazos;

        }

        /// <summary>
        /// Consulta Plazos  para 2X1 y computo 
        /// </summary>
        /// <param name="clasificacion"></param>
        /// <param name="plazMin"></param>
        /// <param name="plaMax"></param>
        /// <param name="fecha"></param>
        /// <param name="cadena"></param>
        /// <param name="tipoCalculo"></param>
        /// <param name="periodo"></param>
        /// <returns></returns>
        public EntPlazosVenta ConsultaPlazosPromociones(string clasificacion, int plazMin, int plaMax, string fecha, string cadena, int tipoCalculo, int periodo)
        {
            var metodo = this.GetType().Name + "." + System.Reflection.MethodBase.GetCurrentMethod().Name;
            _logs.AppendLine(metodo, "Inicia");

            EntPlazosVenta LstplazosResponse = new EntPlazosVenta();
            try
            {
                //Se realiza la validación de la cadena de productos
                if (String.IsNullOrEmpty(cadena) || cadena.Trim().Length == 0)
                {
                    LstplazosResponse.oEntRespuestaNST.eTipoError = EnumTipoErrorNST.Error;
                    LstplazosResponse.oEntRespuestaNST.mensajeError = "Favor de enviar una cadena de sku para poder calcular los abonos.";
                    return LstplazosResponse;
                }
                // Se crea objeto de POS.Telefonia.Negocio.ServicioTienda
                AbonosPromo abonosPromos = new AbonosPromo();
                LstplazosResponse = abonosPromos.ObtenerAbonosPromociones(clasificacion, plazMin, plaMax, fecha, cadena, tipoCalculo, periodo);
                _logs.AppendLineJson(metodo, "Lista plazos: ", new { LstplazosResponse = LstplazosResponse });

            }
            catch (Exception ex)
            {
                LstplazosResponse = new EntPlazosVenta();
                LstplazosResponse.oEntRespuestaNST.eTipoError = EnumTipoErrorNST.Error;
                LstplazosResponse.oEntRespuestaNST.mensajeError = "Error al consultar los plazos calculados.";
                System.Diagnostics.Trace.WriteLine(metodo + " Error al consultar los plazos calculados. Mensaje: " + ex.Message + " Trace: " + ex.StackTrace, "LOG");
                _logs.AppendLineError(metodo, "Error al consultar los plazos calculados. Mensaje: " + ex.Message + " Trace: " + ex.StackTrace);
                _logs.AppendLine(metodo, "Termina en error");
                _logs.EscribeLog();
            }
            _logs.AppendLine(metodo, "Termina");
            return LstplazosResponse;

        }


        public EntRespuestaCotizarVentasCredNST CotizarVentasCredito(List<EntDetalleVentaBaseNST> lstEntDetalleVentaBaseNST, int plazoSeleccionado, EntEngancheNST oEntEngancheNST, List<EntPromocionEspecialNST> lstEntPromocionEspecialNST, EntVentaEmpleadoNST oEntVentaEmpleadoNST, EntComplementosNST oEntComplementosNST)
        {
            return CotizarVentasCredito(lstEntDetalleVentaBaseNST, plazoSeleccionado, null, null, oEntEngancheNST, lstEntPromocionEspecialNST, oEntVentaEmpleadoNST, oEntComplementosNST);
        }

        public EntRespuestaCotizarVentasCredNST CotizarVentasCredito(List<EntDetalleVentaBaseNST> lstEntDetalleVentaBaseNST, int plazoSeleccionado, EntClienteNST oEntClienteNST, EntAccionesCreditoNST oEntAccionesCreditoNST, EntEngancheNST oEntEngancheNST, List<EntPromocionEspecialNST> lstEntPromocionEspecialNST, EntVentaEmpleadoNST oEntVentaEmpleadoNST, EntComplementosNST oEntComplementosNST)
        {
            var metodo = this.GetType().Name + "." + System.Reflection.MethodBase.GetCurrentMethod().Name;
            _logs.AppendLine(metodo, "Inicia");
            System.Diagnostics.Trace.WriteLine("CotizarVentasCredito() oEntClienteNST" + ManejadorInformacion.Serialize_Array(oEntClienteNST, this.oldString), "LOG");
            WCFServicioTienda wsWCFServicioTienda = new WCFServicioTienda();
            RespuestaCotizarVentas oRespuestaCotizarVentas = new RespuestaCotizarVentas();
            EntRespuestaCotizarVentasCredNST oEntRespuestaCotizarVentasCredNST = new EntRespuestaCotizarVentasCredNST();
            //Se inicializa la variable para mostrar popup
            oEntRespuestaCotizarVentasCredNST.oEntEngancheNST.oEntComplemenTel.esMostrarVentanaCreditoGratis = false;
            EntVentaActualCotizar[] lstEntVentaActualCotizar;
            ManejadorServicioTienda manejadorServicioTienda = new ManejadorServicioTienda();
            bool EsProdUOIFI = false;
            bool EsRecargaInicial = false;
            bool HaySkuTelcel = false;
			bool HaySkuTelcelLibre = false;

            bool recInicial = false;
			bool recInicialLibre = false;
            bool activacionTelcel = false;
			bool activacionLibre = false;
            bool esValidaRecInicial = false;
			bool esValidaRecInicialLibre = false;
            int skuRecargaInicial = 0;
			int skuRecargaInicialLibre = 0;
            int countTelcel = 0;
            int countRecargaInicial = 0;

            int EsBlackList = 0;

            bool EsRecargaInicialVal = false;
            int HaySKURecinicial = 0;
            int HaySKURecinicialLibre = 0;
            int PrecioMinimoEquipo = 1998;
            int precioCredito = 0;

            int mostrarCreditoGratis = 0;
            bool esCreditoGratis = false;

            string mensajeCombosPUC = "";
            int countValidadorPUC = 0;
            int skuRecargaLibre = 0;
			bool esLibreNoActivo = false; 
            bool validaTelcel = false; 

            try
            {
                #region configRecargainicial(0)
                DataSet configRecargaInicial = new DataSet();
				DataSet configRecargaInicialLibre = new DataSet();
                DataSet creditoGratis = new DataSet();

                //Se valida dentro de la lista que contenga un sku telcel para enviarlo a validar con recarga inicial
                for (int i = 0; i < lstEntDetalleVentaBaseNST.Count; i++)
                {

                    if (CtrlReglasGenericas.AplicarRegla(437, lstEntDetalleVentaBaseNST[i].SKU))
                    {
                        HaySKURecinicial = lstEntDetalleVentaBaseNST[i].SKU;
                        precioCredito = Convert.ToInt32(lstEntDetalleVentaBaseNST[i].precioLista);
                        System.Diagnostics.Trace.WriteLine(metodo + "precioCredito: " + lstEntDetalleVentaBaseNST[i].precioLista, "LOG");
                        validaTelcel = true;
                        System.Diagnostics.Trace.WriteLine(metodo + "esTelcel : " + validaTelcel, "LOG");
                    }
                    //Valida  is existe  1  CHip  0 para a recarga de Paquete en  abierta 
                    if (CtrlReglasGenericas.AplicarRegla(519, lstEntDetalleVentaBaseNST[i].SKU))
                    {
                        for (int j = 0; j < lstEntDetalleVentaBaseNST[i].lstEntPromocionAplicadaNST.Count; j++)
                        {
                            if (CtrlReglasGenericas.AplicarRegla(775, lstEntDetalleVentaBaseNST[i].lstEntPromocionAplicadaNST[j].skuRegalo))
                            {
                                HaySKURecinicialLibre = lstEntDetalleVentaBaseNST[i].SKU;
                                precioCredito = Convert.ToInt32(lstEntDetalleVentaBaseNST[i].precioLista);
                                System.Diagnostics.Trace.WriteLine(metodo + "Para el tema de regalo precioCredito: " + lstEntDetalleVentaBaseNST[i].precioLista, "LOG");
                                HaySkuTelcelLibre = true;
                                System.Diagnostics.Trace.WriteLine(metodo + "HaySkuTelcelLibre: " + HaySkuTelcelLibre, "LOG");
                            }
                        }
                    }
                }
                System.Diagnostics.Trace.WriteLine(metodo + " HaySKURecinicial Blacklist: " + HaySKURecinicial.ToString(), "LOG");
                System.Diagnostics.Trace.WriteLine(metodo + " HaySKURecinicialLibre  Blacklist: " + HaySKURecinicialLibre.ToString(), "LOG");
                configRecargaInicial = this.GetConfigRecargainicial(HaySKURecinicial);

                if (configRecargaInicial != null && configRecargaInicial.Tables[0] != null && configRecargaInicial.Tables.Count > 0 && configRecargaInicial.Tables[0].Rows.Count > 0)
                {
                    recInicial = Convert.ToBoolean(configRecargaInicial.Tables[0].Rows[0]["RecargaInicial"]);
                    activacionTelcel = Convert.ToBoolean(configRecargaInicial.Tables[0].Rows[0]["ActivacionTelcel"]);
                    skuRecargaInicial = Convert.ToInt32(configRecargaInicial.Tables[0].Rows[0]["SkuRecargaInicial"]);   // 33001151
                    EsBlackList = Convert.ToInt32(configRecargaInicial.Tables[0].Rows[0]["EsBlackList"]);

                    if (recInicial && activacionTelcel)
                    {
                        System.Diagnostics.Trace.WriteLine(metodo + " Recarga Inicial: TRUE, Activacion Telcel: TRUE", "LOG");
                        esValidaRecInicial = true;
                    }
                }

                System.Diagnostics.Trace.WriteLine(metodo + " Validacion recarga para telefonia  libre  " + HaySKURecinicialLibre.ToString(), "LOG");
                configRecargaInicialLibre = this.GetConfigRecargainicialLibre(HaySKURecinicialLibre);
                if (configRecargaInicialLibre != null && configRecargaInicialLibre.Tables[0] != null && configRecargaInicialLibre.Tables.Count > 0 && configRecargaInicialLibre.Tables[0].Rows.Count > 0)
                {
                    recInicialLibre = Convert.ToBoolean(configRecargaInicialLibre.Tables[0].Rows[0]["RecargaInicial"]);
                    activacionTelcel = Convert.ToBoolean(configRecargaInicialLibre.Tables[0].Rows[0]["ActivacionTelcel"]);
                    activacionLibre = Convert.ToBoolean(configRecargaInicialLibre.Tables[0].Rows[0]["ActivacionLibre"]);
                    skuRecargaLibre = Convert.ToInt32(configRecargaInicialLibre.Tables[0].Rows[0]["SkuRecargaInicial"]);  
                    EsBlackList = Convert.ToInt32(configRecargaInicialLibre.Tables[0].Rows[0]["EsBlackList"]);

                    if (recInicialLibre && activacionTelcel  && activacionLibre )
                    {
                        System.Diagnostics.Trace.WriteLine(metodo + " Recarga Inicial  Libre : TRUE, Activacion Telcel: TRUE, Activacion Telefonia Libre : TRUE ", "LOG");
                        esValidaRecInicialLibre = true;
                    }
                }

                EsRecargaInicialVal = recInicial;
                System.Diagnostics.Trace.WriteLine(metodo + " recInicial       : " + recInicial.ToString(), "LOG");
                System.Diagnostics.Trace.WriteLine(metodo + " activacionTelcel : " + activacionTelcel.ToString(), "LOG");
                System.Diagnostics.Trace.WriteLine(metodo + " EsRecargaInicialVal : " + EsRecargaInicialVal.ToString(), "LOG");
                System.Diagnostics.Trace.WriteLine(metodo + " skuRecargaInicial: " + skuRecargaInicial.ToString(), "LOG");
                System.Diagnostics.Trace.WriteLine(metodo + " EsBlackList: " + EsBlackList.ToString(), "LOG");
                System.Diagnostics.Trace.WriteLine("SKU de recarga Libre " + skuRecargaLibre, "log");

                //Se realizan validaciones para saber si se muestra o no el popup de credito gratis
                creditoGratis = this.GetMostrarCreditoGratis();
                //configRecargaInicial = this.GetConfigRecargainicial(HaySKURecinicial);
                if (creditoGratis != null && creditoGratis.Tables[0] != null && creditoGratis.Tables.Count > 0 && creditoGratis.Tables[0].Rows.Count > 0)
                {
                    mostrarCreditoGratis = Convert.ToInt32(creditoGratis.Tables[0].Rows[0]["fiSubItemStat"]);
                    System.Diagnostics.Trace.WriteLine(metodo + "mostrarCreditoGratis: " + mostrarCreditoGratis, "LOG");

                    if (mostrarCreditoGratis > 0)
                    {
                        System.Diagnostics.Trace.WriteLine(metodo + " Se debe mostrar popup credito gratis", "LOG");
                        //oEntRespuestaCotizarVentasCredNST.oEntEngancheNST.oEntComplemenTel.esMostrarVentanaCreditoGratis = true;
                        esCreditoGratis = true;
                    }
                    else
                    {
                        System.Diagnostics.Trace.WriteLine(metodo + " No se muestra popup credito gratis", "LOG");
                        //oEntRespuestaCotizarVentasCredNST.oEntEngancheNST.oEntComplemenTel.esMostrarVentanaCreditoGratis = false;
                        esCreditoGratis = false;
                    }
                }
                else
                {
                    System.Diagnostics.Trace.WriteLine(metodo + " No se muestra popup credito gratis, no hay datos en tabla Catalogo_Generico, fiItemId = 9", "LOG");
                    //oEntRespuestaCotizarVentasCredNST.oEntEngancheNST.oEntComplemenTel.esMostrarVentanaCreditoGratis = false;
                    esCreditoGratis = false;
                }
                //Terminan validaciones popup

                #endregion

                if (oEntComplementosNST.oAddressClient == null)
                {
                    for (int i = 0; i < lstEntDetalleVentaBaseNST.Count; i++)
                    {
                        if (CtrlReglasGenericas.AplicarRegla(829, lstEntDetalleVentaBaseNST[i].SKU))
                        {
                            EsProdUOIFI = true;
                            break;
                        }

                        if (CtrlReglasGenericas.AplicarRegla(437, lstEntDetalleVentaBaseNST[i].SKU))
                        {
                            HaySkuTelcel = true;
                            countTelcel = countTelcel + lstEntDetalleVentaBaseNST[i].Cantidad;
                        }
                        //Se valida si existe un chip 0 en telefonia  abierta si es correcto  se incluye en la recarga
                        if (CtrlReglasGenericas.AplicarRegla(519, lstEntDetalleVentaBaseNST[i].SKU) && esValidaRecInicialLibre)
                        {
                            System.Diagnostics.Trace.WriteLine(metodo + " Es telefonia abierta : " + lstEntDetalleVentaBaseNST[i].SKU, "LOG");
                            for (int h = 0; h < lstEntDetalleVentaBaseNST[i].lstEntPromocionAplicadaNST.Count; h++)
                            {
                                System.Diagnostics.Trace.WriteLine(metodo + " tiene Promocion " + lstEntDetalleVentaBaseNST[i].lstEntPromocionAplicadaNST[h].promocionId, "LOG");

                                if (CtrlReglasGenericas.AplicarRegla(775, lstEntDetalleVentaBaseNST[i].lstEntPromocionAplicadaNST[h].skuRegalo))
                                {
                                    System.Diagnostics.Trace.WriteLine(metodo + " Es telefonia SIM 0 : " + lstEntDetalleVentaBaseNST[i].lstEntPromocionAplicadaNST[h].skuRegalo, "LOG");
                                    HaySkuTelcelLibre = true;
                                    countTelcel = countTelcel + lstEntDetalleVentaBaseNST[i].Cantidad;
                                }
                            }

                        }
                    }

                    if (EsProdUOIFI)
                    {
                        _logs.AppendLineError(metodo, "Se intenta cotizar productos de OUIFI desde EPOS");
                        oEntRespuestaCotizarVentasCredNST.oEntRespuestaNST.eTipoError = EnumTipoErrorNST.Error;
                        oEntRespuestaCotizarVentasCredNST.oEntRespuestaNST.mensajeError = "Estos productos no pueden ser vendidos por el cotizador de EPOS.";
                        _logs.EscribeLog();
                        return oEntRespuestaCotizarVentasCredNST;
                    }

                    #region validacionesRecargaInicial
                    System.Diagnostics.Trace.WriteLine(metodo + " countTelcel: " + countTelcel.ToString(), "LOG");

                    // VALIDA SI EN LA COTIZACION NO HAY EQUIPOS DE TELCEL
                    if (!HaySkuTelcel)
                    {
                        System.Diagnostics.Trace.WriteLine(metodo + " NO HaySkuTelcel", "LOG");

                        System.Diagnostics.Trace.WriteLine(metodo + "lstEntDetalleVentaBaseNST.Count: " + lstEntDetalleVentaBaseNST.Count.ToString(), "LOG");

                        for (int i = 0; i < lstEntDetalleVentaBaseNST.Count; i++)
                        {
                            System.Diagnostics.Trace.WriteLine(metodo + "lstEntDetalleVentaBaseNST.SKU: " + lstEntDetalleVentaBaseNST[i].SKU, "LOG");
                            if (lstEntDetalleVentaBaseNST[i].SKU == skuRecargaInicial)
                            {
                                EsRecargaInicial = true;
                                System.Diagnostics.Trace.WriteLine(metodo + " EsRecargaInicial: true", "LOG");
                                break;
                            }
                            if (lstEntDetalleVentaBaseNST[i].SKU == skuRecargaLibre)
                            {
                                EsRecargaInicial = true;
                                System.Diagnostics.Trace.WriteLine(metodo + " skuRecargaLibre: true", "LOG");
                                break;
                            }
                        }
                    }
                    else
                    {
                        System.Diagnostics.Trace.WriteLine(metodo + " HaySkuTelcel: true", "LOG");
                        for (int i = 0; i < lstEntDetalleVentaBaseNST.Count; i++)
                        {
                            if (lstEntDetalleVentaBaseNST[i].SKU == skuRecargaInicial)
                            {
                                EsRecargaInicial = true;
                                System.Diagnostics.Trace.WriteLine(metodo + " EsRecargaInicial: true", "LOG");

                                if (!esValidaRecInicial)
                                {
                                    System.Diagnostics.Trace.WriteLine(metodo + " La sucursal no esta habilitada para Recarga Inicial o Activacion de Telcel", "LOG");
                                    break;
                                }

                                countRecargaInicial = countRecargaInicial + lstEntDetalleVentaBaseNST[i].Cantidad;
                                System.Diagnostics.Trace.WriteLine(metodo + " countRecargaInicial: " + countRecargaInicial.ToString(), "LOG");
                            }
                        }
                    }

                    System.Diagnostics.Trace.WriteLine(metodo + " EsRecargaInicial  : " + EsRecargaInicial.ToString(), "LOG");
                    System.Diagnostics.Trace.WriteLine(metodo + " HaySkuTelcel      : " + HaySkuTelcel.ToString(), "LOG");
                    System.Diagnostics.Trace.WriteLine(metodo + " esValidaRecInicial: " + esValidaRecInicial.ToString(), "LOG");
                    System.Diagnostics.Trace.WriteLine(metodo + " HaySkuTelcelLibre: " + HaySkuTelcelLibre.ToString(), "LOG");
                    System.Diagnostics.Trace.WriteLine(metodo + " esValidaRecInicialLibre: " + esValidaRecInicialLibre.ToString(), "LOG");

                    if (HaySkuTelcelLibre && !esValidaRecInicialLibre)
                    {
                        esLibreNoActivo = true;
                    }

                    System.Diagnostics.Trace.WriteLine(metodo + " esLibreNoActivo: " + esLibreNoActivo.ToString(), "LOG");
                    //if ((EsRecargaInicial && !HaySkuTelcel) || (EsRecargaInicial && !esValidaRecInicial))
                    if (((EsRecargaInicial && !HaySkuTelcel) || (EsRecargaInicial && !esValidaRecInicial)) && (!HaySkuTelcelLibre || esLibreNoActivo))
                    {
                        System.Diagnostics.Trace.WriteLine(metodo + "Entro en validacion de Telcel", "LOG");
                        System.Diagnostics.Trace.WriteLine(metodo + " Se intenta cotizar Recarga Inicial de Telcel sin un equipo de Telcel en la cotización. ", "LOG");
                        _logs.AppendLineError(metodo, "Se intenta cotizar Recarga Inicial de Telcel sin un equipo de Telcel en la cotización. ");
                        oEntRespuestaCotizarVentasCredNST.oEntRespuestaNST.eTipoError = EnumTipoErrorNST.Error;

                        // ELIMINAR O NO ADMITIR QUE LA RECARGA INICIAL SE AGREGUE EN LA COTIZACION
                        for (int i = 0; i < lstEntDetalleVentaBaseNST.Count; i++)
                        {
                            if (lstEntDetalleVentaBaseNST[i].SKU == skuRecargaInicial)
                            {
                                lstEntDetalleVentaBaseNST.Remove(lstEntDetalleVentaBaseNST[i]);
                            }
                            if (skuRecargaInicial != skuRecargaLibre)
                            {
                                if (lstEntDetalleVentaBaseNST[i].SKU == skuRecargaLibre)
                                {
                                    lstEntDetalleVentaBaseNST.Remove(lstEntDetalleVentaBaseNST[i]);
                                }
                            }
                        }
                        for (int i = 0; i < oEntRespuestaCotizarVentasCredNST.lstEntDetalleVentaResNST.Count; i++)
                        {
                            if (oEntRespuestaCotizarVentasCredNST.lstEntDetalleVentaResNST[i].SKU == skuRecargaInicial)
                            {
                                oEntRespuestaCotizarVentasCredNST.lstEntDetalleVentaResNST.Remove(oEntRespuestaCotizarVentasCredNST.lstEntDetalleVentaResNST[i]);
                            }
                            if (skuRecargaInicial != skuRecargaLibre)
                            {
                                if (oEntRespuestaCotizarVentasCredNST.lstEntDetalleVentaResNST[i].SKU == skuRecargaLibre)
                                {
                                    oEntRespuestaCotizarVentasCredNST.lstEntDetalleVentaResNST.Remove(oEntRespuestaCotizarVentasCredNST.lstEntDetalleVentaResNST[i]);
                                }
                            }
                        }


                        if (esLibreNoActivo)
                        {
                            System.Diagnostics.Trace.WriteLine(metodo + " La sucursal no esta habilitada para Recarga Inicial, Activacion Telcel en Telefonia Libre o Activacion de Telcel.", "LOG");
                            oEntRespuestaCotizarVentasCredNST.oEntRespuestaNST.mensajeError = "La sucursal no esta habilitada para Recarga Inicial o Activacion de Telcel.";
                            _logs.EscribeLog();
                            return oEntRespuestaCotizarVentasCredNST;
                        }

                        // SE ENVIA MENSAJE POR LA CAUSA
                        if (!HaySkuTelcel  )
                        {
                            System.Diagnostics.Trace.WriteLine(metodo + " La Recarga Inicial no pueden ser vendida en el cotizador de EPOS, sin un equipo de Telcel agregado en la cotizacion.", "LOG");
                            oEntRespuestaCotizarVentasCredNST.oEntRespuestaNST.mensajeError = "La Recarga Inicial no pueden ser vendida en el cotizador de EPOS, sin un equipo de Telcel agregado en la cotizacion.";
                        }
                        else
                        {
                            System.Diagnostics.Trace.WriteLine(metodo + " La sucursal no esta habilitada para Recarga Inicial o Activacion de Telcel.", "LOG");
                            oEntRespuestaCotizarVentasCredNST.oEntRespuestaNST.mensajeError = "La sucursal no esta habilitada para Recarga Inicial o Activacion de Telcel.";
                        }

                        _logs.EscribeLog();
                        return oEntRespuestaCotizarVentasCredNST;
                    }

                    if (countRecargaInicial > countTelcel)
                    {
                        System.Diagnostics.Trace.WriteLine(metodo + " countRecargaInicial: " + countRecargaInicial.ToString(), "LOG");
                        System.Diagnostics.Trace.WriteLine(metodo + " countTelcel: " + countTelcel.ToString(), "LOG");

                        for (int i = 0; i < lstEntDetalleVentaBaseNST.Count; i++)
                        {
                            if (lstEntDetalleVentaBaseNST[i].SKU == skuRecargaInicial)
                            {
                                lstEntDetalleVentaBaseNST[i].Cantidad = countTelcel;
                                System.Diagnostics.Trace.WriteLine(metodo + " se actualiza al maximo de equipos telcel en la cotizacion: " + lstEntDetalleVentaBaseNST[i].Cantidad, "LOG");
                                break;
                            }
                            if (lstEntDetalleVentaBaseNST[i].SKU == skuRecargaLibre)
                            {
                                lstEntDetalleVentaBaseNST[i].Cantidad = countTelcel;
                                System.Diagnostics.Trace.WriteLine(metodo + " se actualiza al maximo de equipos telcel en la cotizacion: " + lstEntDetalleVentaBaseNST[i].Cantidad, "LOG");
                                break;
                            }
                        }
                    }
                    #endregion
                }

                if (lstEntDetalleVentaBaseNST != null)
                {
                    for (int i = 0; i < lstEntDetalleVentaBaseNST.Count; i++)
                    {
                        if (CtrlReglasGenericas.AplicarRegla(677, lstEntDetalleVentaBaseNST[i].SKU))
                        {
                            if (!oEntVentaEmpleadoNST.DescCompania.Contains("Portal_OUI"))
                            {
                                System.Diagnostics.Trace.WriteLine("Se intenta cotizar sim card de OUI desde EPOS", "LOG");
                                oEntRespuestaCotizarVentasCredNST.oEntRespuestaNST.eTipoError = EnumTipoErrorNST.Error;
                                oEntRespuestaCotizarVentasCredNST.oEntRespuestaNST.mensajeError = "Estos productos no pueden ser vendidos por el cotizador de EPOS.";
                                _logs.EscribeLog();
                                return oEntRespuestaCotizarVentasCredNST;
                            }
                            break;
                        }
                        if (lstEntDetalleVentaBaseNST[i].eTipoProductoNST == EnumTipoProductoNST.telefonia)
                        {
                            if (!oEntAccionesCreditoNST.esVentaEngCero && oEntClienteNST.CteSinPedidos)
                            {
                                oEntClienteNST.CteSinPedidos = false;
                            }
                        }
                    }
                }
                new ManejadorPromocionesNST().LimpiarPromociones(lstEntDetalleVentaBaseNST);
                this.lstDetalleVentaBonoAux = this.CrearDetalleVentaBono(lstEntDetalleVentaBaseNST, false);
                this.CrearDetalleSeguroVidaMax(lstEntDetalleVentaBaseNST, plazoSeleccionado, false);
                oEntRespuestaCotizarVentasCredNST.oEntRespuestaNST = new ManejadorVentaNST().ValidarConvivenciaProductos(lstEntDetalleVentaBaseNST);
                if (lstEntPromocionEspecialNST != null)
                    new ManejadorPromocionesEspecialesNST().AgregarPromocionesEspeciales(lstEntPromocionEspecialNST, lstEntDetalleVentaBaseNST, EnumTipoVentaNST.Credito);
                lstEntVentaActualCotizar = this.CreaLstEntVentaActualCotizar(lstEntDetalleVentaBaseNST, EnumTipoVenta.credito, oEntEngancheNST, plazoSeleccionado, oEntComplementosNST);
                InformacionEmpleado oInformacionEmpleado = this.CrearInformacionEmpleado(oEntVentaEmpleadoNST);
                ClienteIpadBase oClienteIpadBase = new ClienteIpadBase();
                oClienteIpadBase = this.CrearClienteIpadBase(oEntClienteNST, oEntAccionesCreditoNST, oEntComplementosNST);

                var Atributos = new List<Atributo>();

                #region validacionesClienteRecomendado

                if (oEntClienteNST.folioCU > 0 && (oEntClienteNST.DatosRecomendador.codigoCanje != null
                    || oEntClienteNST.DatosRecomendador.codigoCanje != "" || oEntClienteNST.DatosRecomendador.codigoCanje != " "
                    || oEntClienteNST.DatosRecomendador.codigoCanje.Length > 0))
                {
                    System.Diagnostics.Trace.WriteLine("Entra a cliente recomendador ", "LOG");


                    System.Diagnostics.Trace.WriteLine("clienteUnico: " + oEntClienteNST.DatosRecomendador.clienteUnico, "LOG");
                    System.Diagnostics.Trace.WriteLine("codigoCanje: " + oEntClienteNST.DatosRecomendador.codigoCanje, "LOG");
                    System.Diagnostics.Trace.WriteLine("portalVenta: " + oEntClienteNST.DatosRecomendador.portalVenta, "LOG");
                    System.Diagnostics.Trace.WriteLine("montoVenta: " + oEntClienteNST.DatosRecomendador.montoVenta, "LOG");
                    System.Diagnostics.Trace.WriteLine("tipoCliente " + oEntClienteNST.DatosRecomendador.tipoCliente, "LOG");

                    oEntClienteNST.DatosRecomendador.tipoCliente = oEntClienteNST.idDescuentoApi;

                    /*System.Diagnostics.Trace.WriteLine("Se realiza Inserción PromocionPDVTelOUI ", "LOG");
                    string valData = JsonConvert.SerializeObject(oEntClienteNST.DatosRecomendador);                     
                    System.Diagnostics.Trace.WriteLine("valData: " + valData, "LOG");
                    Atributos.Add(new Atributo { Key = "PromocionPDVTelOUI", Value = valData });                   */
                    System.Diagnostics.Trace.WriteLine("valor KEY para atributo: CotizaTelefoniaPromocionPDVTelOUI ", "LOG");
                    string valData = ObjectToXML(oEntClienteNST.DatosRecomendador);
                    System.Diagnostics.Trace.WriteLine("Se genera xml: " + valData, "LOG");

                    Atributos.Add(new Atributo { Key = "CotizaTelefoniaPromocionPDVTelOUI", Value = valData });
                    if (oEntClienteNST.lstDescuentos == null)
                        oEntClienteNST.lstDescuentos = new List<lstDescuentoReno>();
                    oEntClienteNST.lstDescuentos.Add(new lstDescuentoReno { descripcion = "idDescuento", valor = oEntClienteNST.idDescuentoApi.ToString() });


                }
                #endregion

                //Renovaciones Prepago Telefonia 
                List<EntParamAPI> lstEntParamAPI = new List<EntParamAPI>();
                if (oEntClienteNST.lstDescuentos != null && oEntClienteNST.lstDescuentos.Count > 0)
                {
                    for (int i = 0; i < oEntClienteNST.lstDescuentos.Count; i++)
                    {
                        if (lstEntVentaActualCotizar[0].oEntParamAPI == null)
                            lstEntVentaActualCotizar[0].oEntParamAPI = new EntParamAPI[oEntClienteNST.lstDescuentos.Count];

                        if (oEntClienteNST.lstDescuentos[i].descripcion == "tipoPromo")
                        {
                            tipoPromocion = oEntClienteNST.lstDescuentos[i].valor;
                        }

                        EntParamAPI consulDescuento = new EntParamAPI();
                        consulDescuento.descripcion = oEntClienteNST.lstDescuentos[i].descripcion;
                        consulDescuento.valor = oEntClienteNST.lstDescuentos[i].valor;

                        lstEntVentaActualCotizar[0].oEntParamAPI[i] = consulDescuento;
                    }
                }

                //Fin Renovaciones Telefonia                                   
                wsWCFServicioTienda.Timeout = 600000;
                System.Diagnostics.Trace.WriteLine("Antes de consultar wsWCFServicioTienda.CotizarVentas", "LOG");

                //var Atributos = new List<Atributo>();

                if (oEntEngancheNST.oEntComplemenTel == null)
                {
                    oEntEngancheNST.oEntComplemenTel = new EntComplemenTel();
                }

                if (oEntEngancheNST.oEntComplemenTel != null)
                {
                    System.Diagnostics.Trace.WriteLine("1esAgregadaRecargaInicial: " + oEntEngancheNST.oEntComplemenTel.esAgregadaRecargaInicial, "LOG");
                    if (oEntEngancheNST.oEntComplemenTel.esAgregadaRecargaInicial)
                    {
                        Atributos.Add(new Atributo { Key = "esAgregadaRecargaInicial", Value = oEntEngancheNST.oEntComplemenTel.esAgregadaRecargaInicial.ToString() });
                    }

                    System.Diagnostics.Trace.WriteLine("HaySkuTelcel: " + HaySkuTelcel, "LOG");
                    if (HaySkuTelcel)
                    {
                        oEntEngancheNST.oEntComplemenTel.haySkuTelcel = true;
                    }
                    else
                    {
                        oEntEngancheNST.oEntComplemenTel.haySkuTelcel = false;
                    }
                    Atributos.Add(new Atributo { Key = "haySkuTelcel", Value = oEntEngancheNST.oEntComplemenTel.haySkuTelcel.ToString() });

                    System.Diagnostics.Trace.WriteLine("esMostrarVentanaRecargaInicial: " + oEntEngancheNST.oEntComplemenTel.esMostrarVentanaRecargaInicial, "LOG");
                    if (oEntEngancheNST.oEntComplemenTel.esMostrarVentanaRecargaInicial)
                    {
                        System.Diagnostics.Trace.WriteLine("@@Entra: ", "LOG");
                        System.Diagnostics.Trace.WriteLine("@@EsRecargaInicialVal: " + EsRecargaInicialVal, "LOG");
                        System.Diagnostics.Trace.WriteLine(metodo + "2 EsBlackList: " + EsBlackList.ToString(), "LOG");
                        if (EsRecargaInicialVal && EsBlackList == 1 && activacionTelcel && HaySkuTelcel && precioCredito >= PrecioMinimoEquipo)
                        {
                            System.Diagnostics.Trace.WriteLine("@@11: ", "LOG");
                            //oEntEngancheNST.oEntComplemenTel.esMostrarVentanaRecargaInicial = true;
                            Atributos.Add(new Atributo { Key = "esMostrarVentanaRecargaInicial", Value = "true" });
                        }
                        else
                        {
                            System.Diagnostics.Trace.WriteLine("@@12: ", "LOG");
                            //oEntEngancheNST.oEntComplemenTel.esMostrarVentanaRecargaInicial = false;
                            Atributos.Add(new Atributo { Key = "esMostrarVentanaRecargaInicial", Value = "false" });
                            EsRecargaInicialVal = false; //Se setea en falso para no mostrar ventana
                        }
                        //Atributos.Add(new Atributo { Key = "esMostrarVentanaRecargaInicial", Value = oEntEngancheNST.oEntComplemenTel.esMostrarVentanaRecargaInicial.ToString() });
                    }
                    else
                    {
                        if (EsRecargaInicialVal && EsBlackList == 1 && activacionTelcel && HaySkuTelcel && precioCredito >= PrecioMinimoEquipo)
                        {
                            EsRecargaInicialVal = true; //Se setea en falso para no mostrar ventana
                        }
                        else
                        {
                            EsRecargaInicialVal = false; //Se setea en falso para no mostrar ventana
                        }
                    }

                    System.Diagnostics.Trace.WriteLine("yaMostroVentanaRecargaInicial: " + oEntEngancheNST.oEntComplemenTel.yaMostroVentanaRecargaInicial, "LOG");
                    if (oEntEngancheNST.oEntComplemenTel.yaMostroVentanaRecargaInicial)
                    {
                        System.Diagnostics.Trace.WriteLine("if: ", "LOG");
                        Atributos.Add(new Atributo { Key = "yaMostroVentanaRecargaInicial", Value = oEntEngancheNST.oEntComplemenTel.yaMostroVentanaRecargaInicial.ToString() });
                    }
                    else
                    {
                        System.Diagnostics.Trace.WriteLine("else: ", "LOG");
                        if (oEntEngancheNST.oEntComplemenTel.esAgregadaRecargaInicial && oEntEngancheNST.oEntComplemenTel.esMostrarVentanaRecargaInicial)
                        {
                            System.Diagnostics.Trace.WriteLine("cierto: ", "LOG");
                            Atributos.Add(new Atributo { Key = "yaMostroVentanaRecargaInicial", Value = "true" });
                        }
                        else
                        {
                            System.Diagnostics.Trace.WriteLine("falso: ", "LOG");
                            Atributos.Add(new Atributo { Key = "yaMostroVentanaRecargaInicial", Value = "false" });
                        }
                    }
                    System.Diagnostics.Trace.WriteLine("Vaida si se elimina  el SKU si es  recarga inicial libre ", "LOG");
                    if (oEntEngancheNST.oEntComplemenTel.eliminaSKUlibre)
                    {
                        System.Diagnostics.Trace.WriteLine("Aplica elimar recarga: ", "LOG");
                        Atributos.Add(new Atributo { Key = "eliminaSKUlibre", Value = "true" });
                    }
                    else
                    {
                        System.Diagnostics.Trace.WriteLine("El parametro indica que no elimina recarga : ", "LOG");
                        Atributos.Add(new Atributo { Key = "eliminaSKUlibre", Value = "false" });
                    }

                }
                oRespuestaCotizarVentas = wsWCFServicioTienda.CotizarVentas(lstEntVentaActualCotizar, oClienteIpadBase, this.oEntSeguroIpad, Atributos.ToArray(), "", "", "", oInformacionEmpleado);
                System.Diagnostics.Trace.WriteLine("Despues de consultar wsWCFServicioTienda.CotizarVentas", "LOG");
                this.AgregarOmitirPromociones(ref oRespuestaCotizarVentas, lstEntVentaActualCotizar);
                oEntRespuestaCotizarVentasCredNST = this.CrearEntRespuestaCotizarVentasCredNST(oRespuestaCotizarVentas, oEntRespuestaCotizarVentasCredNST.oEntRespuestaNST, oEntComplementosNST);
                this.GuardaDescuentoAbonos(oEntRespuestaCotizarVentasCredNST, oRespuestaCotizarVentas);
                manejadorServicioTienda.ValidaEnganche(oEntRespuestaCotizarVentasCredNST);
                this.RecalculaEnganche(ref oEntEngancheNST, oRespuestaCotizarVentas);
                oEntRespuestaCotizarVentasCredNST.oEntEngancheNST = oEntEngancheNST;
                this.ModificaTipoAgregado(lstEntDetalleVentaBaseNST, oEntRespuestaCotizarVentasCredNST.lstEntDetalleVentaResNST);
                //if (this.lstDetalleVentaBonoAux.Length > 0)
                //    this.ActualizaProductosBono(oRespuestaCotizarVentas.ListaVentaActual[0].ListaDetalleVenta, oEntRespuestaCotizarVentasCredNST.lstEntDetalleVentaResNST);
                oEntRespuestaCotizarVentasCredNST.totalVentaComplementos = this.CalculaTotalComplementos(oEntRespuestaCotizarVentasCredNST.lstEntDetalleVentaResNST);
                oEntRespuestaCotizarVentasCredNST.lstEntPromocionEspecialNST = lstEntPromocionEspecialNST;
                this.ActulizaPromEspecial(oEntRespuestaCotizarVentasCredNST);
                oEntRespuestaCotizarVentasCredNST.oEntVentaEmpleadoNST = oEntVentaEmpleadoNST;
                oEntRespuestaCotizarVentasCredNST.totalVentaBonos = this.ActualizaTotalBono(oEntRespuestaCotizarVentasCredNST.lstEntDetalleVentaResNST);
                oEntRespuestaCotizarVentasCredNST.aplicaPromocionBF = oRespuestaCotizarVentas.cliente.PromocionBFCredito;
                if (oRespuestaCotizarVentas.ListaVentaActual.Length >= 0 && oRespuestaCotizarVentas.ListaVentaActual[0].RecompensasRecomendar != null && oRespuestaCotizarVentas.ListaVentaActual[0].RecompensasRecomendar.RespuestaCaracteristica != null)
                {
                    oEntRespuestaCotizarVentasCredNST.oEntComplementosNST.oEntRecompensasRecomendar = new EntRecompensaRecomendar()
                    {
                        ClienteUnico = oRespuestaCotizarVentas.ListaVentaActual[0].RecompensasRecomendar.ClienteUnico,
                        DescuentoOtorgado = oRespuestaCotizarVentas.ListaVentaActual[0].RecompensasRecomendar.DescuentoOtorgado,
                        EsAplicar = oRespuestaCotizarVentas.ListaVentaActual[0].RecompensasRecomendar.EsAplicar,
                        EsConsultarBanco = oRespuestaCotizarVentas.ListaVentaActual[0].RecompensasRecomendar.EsConsultarBanco,
                        MontoCompra = oRespuestaCotizarVentas.ListaVentaActual[0].RecompensasRecomendar.MontoCompra,
                        MensajeRecompensa = oRespuestaCotizarVentas.ListaVentaActual[0].RecompensasRecomendar.MensajeRecompensa,
                        RespuestaCaracteristica = new EntCaracteristicaRespuesta()
                        {
                            data = new EntCaracteristicaRecompensa()
                            {
                                aplicaRango = oRespuestaCotizarVentas.ListaVentaActual[0].RecompensasRecomendar.RespuestaCaracteristica.AplicaRango,
                                descuentoCada = oRespuestaCotizarVentas.ListaVentaActual[0].RecompensasRecomendar.RespuestaCaracteristica.DescuentoCada,
                                mensaje = oRespuestaCotizarVentas.ListaVentaActual[0].RecompensasRecomendar.RespuestaCaracteristica.Mensaje,
                                montoMinimoCompra = oRespuestaCotizarVentas.ListaVentaActual[0].RecompensasRecomendar.RespuestaCaracteristica.MontoMinimoCompra,
                                montoPremio = oRespuestaCotizarVentas.ListaVentaActual[0].RecompensasRecomendar.RespuestaCaracteristica.MontoPremio,
                                tienePromocion = oRespuestaCotizarVentas.ListaVentaActual[0].RecompensasRecomendar.RespuestaCaracteristica.TienePromocion
                            }
                        }
                    };
                }
                else
                {
                    oEntRespuestaCotizarVentasCredNST.oEntComplementosNST.oEntRecompensasRecomendar = null;
                }
                oEntRespuestaCotizarVentasCredNST.leyendaPromoBF = oRespuestaCotizarVentas.cliente.MsjPromoBFCredito;
                oEntRespuestaCotizarVentasCredNST.msjPromocionTel = oRespuestaCotizarVentas.cliente.msjPromocionTel;
                oEntRespuestaCotizarVentasCredNST.prioridadPromo = oRespuestaCotizarVentas.cliente.prioridadPromo;
                oEntRespuestaCotizarVentasCredNST.aplicaPromociones = oRespuestaCotizarVentas.cliente.aplicaPromociones;
                oEntRespuestaCotizarVentasCredNST.aplicaOcultarServicios = this.BloqueoMileaDesc(oEntRespuestaCotizarVentasCredNST); ;//this.ConsultaOcultarServicios();
                oEntRespuestaCotizarVentasCredNST.aplicaRenovacionTele = oRespuestaCotizarVentas.cliente.aplicaRenovacionTele;
                oEntRespuestaCotizarVentasCredNST.leyenfaRenovacionesTel = oRespuestaCotizarVentas.cliente.leyenfaRenovacionesTel;

                for (int act = 0; act < oEntRespuestaCotizarVentasCredNST.lstEntDetalleVentaResNST.Count; act++)
                    if (CtrlReglasGenericas.AplicarRegla(7, oEntRespuestaCotizarVentasCredNST.lstEntDetalleVentaResNST[act].SKU) == true)
                    {
                        oEntRespuestaCotizarVentasCredNST.lstEntDetalleVentaResNST[act].accionItk = oRespuestaCotizarVentas.ListaVentaActual[0].ListaDetalleVenta[act].accionItk;
                        break;
                    }

                if (EsRecargaInicial && esValidaRecInicial && skuRecargaInicial > 0)
                {
                    var LogsTel = new LogsTelefonia
                    {
                        nombreLog = "RecargInicial.log",
                        rutaLogs = @"E:\ADN\NET64_EKT\Telefonia\PuntoDeVenta\Logs\",
                        tamanioMaximoLog = 1,
                        unidadTamanioMaximoLog = EnumTamanioMaximo.Mega
                    };
                    LogsTel.AppendLine(metodo, "Antes de SetBanderaRecargaInicial");
                    SetBanderaRecargaInicial(oEntRespuestaCotizarVentasCredNST, skuRecargaInicial, LogsTel);
                    LogsTel.AppendLineJson(metodo, "oEntRespuestaCotizarVentasCredNST", oEntRespuestaCotizarVentasCredNST);
                    LogsTel.EscribeLog();
                }

                System.Diagnostics.Trace.WriteLine("esAgregadaRecargaInicial: " + oEntRespuestaCotizarVentasCredNST.oEntEngancheNST.oEntComplemenTel.esAgregadaRecargaInicial, "LOG");
                if (oEntRespuestaCotizarVentasCredNST.oEntEngancheNST.oEntComplemenTel.esAgregadaRecargaInicial)
                {
                    System.Diagnostics.Trace.WriteLine("@1: ", "LOG");
                    oEntRespuestaCotizarVentasCredNST.oEntEngancheNST.oEntComplemenTel.esMostrarVentanaRecargaInicial = false;
                }
                else
                {
                    System.Diagnostics.Trace.WriteLine("@2 ", "LOG");
                    System.Diagnostics.Trace.WriteLine("@EsRecargaInicialVal: " + EsRecargaInicialVal, "LOG");
                    oEntRespuestaCotizarVentasCredNST.oEntEngancheNST.oEntComplemenTel.esMostrarVentanaRecargaInicial = EsRecargaInicialVal;
                }

                System.Diagnostics.Trace.WriteLine(metodo + "@@@esCreditoGratis: " + esCreditoGratis, "LOG");
                oEntRespuestaCotizarVentasCredNST.oEntEngancheNST.oEntComplemenTel.esMostrarVentanaCreditoGratis = esCreditoGratis;
                System.Diagnostics.Trace.WriteLine(metodo + "@@@mostrarCreditoGratis: " + oEntRespuestaCotizarVentasCredNST.oEntEngancheNST.oEntComplemenTel.esMostrarVentanaCreditoGratis, "LOG");

				configRecargaInicialLibre = this.GetConfigRecargainicialLibre(0);

                if (configRecargaInicialLibre != null && configRecargaInicialLibre.Tables[0] != null && configRecargaInicialLibre.Tables.Count > 0 && configRecargaInicialLibre.Tables[0].Rows.Count > 0)
                {
                    skuRecargaInicialLibre = Convert.ToInt32(configRecargaInicial.Tables[0].Rows[0]["SkuRecargaInicial"]);
                    System.Diagnostics.Trace.WriteLine(metodo + " skuRecargaInicial para Telefonia  Libre : " + skuRecargaInicialLibre.ToString(), "LOG");
                }
                System.Diagnostics.Trace.WriteLine(metodo + "@@@Valor de SKU  de recarga  para Telefonia  Libre : " + skuRecargaInicialLibre , "LOG");
                oEntRespuestaCotizarVentasCredNST.oEntEngancheNST.oEntComplemenTel.skuTelefoniaLibre = skuRecargaInicialLibre;
                System.Diagnostics.Trace.WriteLine(metodo + "@@@SKU recarga Telefonia  libre " + oEntRespuestaCotizarVentasCredNST.oEntEngancheNST.oEntComplemenTel.skuTelefoniaLibre, "LOG");
                #region combosPUC
                DataSet configModalCombosPUC = new DataSet();
                //Manda a validar que aplique la promo para PUC
                if (lstEntDetalleVentaBaseNST.Count > 0)
                {
                    System.Diagnostics.Trace.WriteLine(metodo + " Entra en SKU > 0 para buscar combos PUC", "LOG");
                    for (int i = 0; i < lstEntDetalleVentaBaseNST.Count; i++)
                    {
                        if (countValidadorPUC == 0)
                        {
                            configModalCombosPUC = this.GetMostrarModalCombosPUC(lstEntDetalleVentaBaseNST[i].SKU);

                            if (configModalCombosPUC != null && configModalCombosPUC.Tables[0] != null && configModalCombosPUC.Tables.Count > 0 && configModalCombosPUC.Tables[0].Rows.Count > 0)
                            {
                                System.Diagnostics.Trace.WriteLine(metodo + " Se encontro el SKU: " + lstEntDetalleVentaBaseNST[i].SKU + ", aplica con promo combos PUC", "LOG");
                                mensajeCombosPUC = Convert.ToString(configModalCombosPUC.Tables[0].Rows[0]["Mensaje"]);   // Para Combos PUC
                                System.Diagnostics.Trace.WriteLine(metodo + " Mensaje devuelto: " + mensajeCombosPUC, "LOG");
                                oEntRespuestaCotizarVentasCredNST.oEntEngancheNST.oEntComplemenTel.mensajePromoPUC = mensajeCombosPUC;
                                System.Diagnostics.Trace.WriteLine(metodo + " Valor en entidad: " +
                                oEntRespuestaCotizarVentasCredNST.oEntEngancheNST.oEntComplemenTel.mensajePromoPUC, "LOG");
                            }
                        }
                        countValidadorPUC++;
                    }
                }
                #endregion

            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine("Ocurrio un error en NegocioNew. CotizarVentasCredito. Mensaje: " + ex.Message + ", stack:" + ex.StackTrace, "LOG");
                _logs.AppendLineError(metodo, ex.Message + ", stack:" + ex.StackTrace);
                oEntRespuestaCotizarVentasCredNST.oEntRespuestaNST.eTipoError = EnumTipoErrorNST.Error;
                oEntRespuestaCotizarVentasCredNST.oEntRespuestaNST.mensajeError = ex.Message;
                _logs.EscribeLog();
            }
            _logs.AppendLine(metodo, "Termina");
            return oEntRespuestaCotizarVentasCredNST;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="oEntRespuestaCotizarVentasCredNST"></param>
        /// <param name="SKURecarga"></param>
        /// <param name="LogsTel"></param>
        private void SetBanderaRecargaInicial(EntRespuestaCotizarVentasCredNST oEntRespuestaCotizarVentasCredNST, int SKURecarga, LogsTelefonia LogsTel)
        {
            var metodo = this.GetType().Name + "." + System.Reflection.MethodBase.GetCurrentMethod().Name;
            LogsTel.AppendLine(metodo, "Inicia");

            if (oEntRespuestaCotizarVentasCredNST.oEntEngancheNST == null || oEntRespuestaCotizarVentasCredNST.oEntEngancheNST.oEntComplemenTel == null)
            {
                System.Diagnostics.Trace.WriteLine(metodo + " oEntEngancheNST.oComplemenTel: Se instancia", "LOG");
                LogsTel.AppendLine(metodo, " oEntEngancheNST.oComplemenTel: Se instancia");
                oEntRespuestaCotizarVentasCredNST.oEntEngancheNST.oEntComplemenTel = new EntComplemenTel();
            }

            if (oEntRespuestaCotizarVentasCredNST.oEntEngancheNST.oEntComplemenTel.esAgregadaRecargaInicial == false)
            {
                if (oEntRespuestaCotizarVentasCredNST.lstEntDetalleVentaResNST.Exists(x => x.SKU == SKURecarga))
                {
                    oEntRespuestaCotizarVentasCredNST.oEntEngancheNST.oEntComplemenTel.esAgregadaRecargaInicial = true;
                    LogsTel.AppendLine(metodo, " oEntEngancheNST.oEntComplemenTel.esAgregadaRecargaInicial: Se agrego una Recarga inicial");
                    System.Diagnostics.Trace.WriteLine(metodo + " oEntEngancheNST.oEntComplemenTel.esAgregadaRecargaInicial: Se agrego una Recarga inicial", "LOG");
                }
            }
        }

        private void AgregarOmitirPromociones(ref RespuestaCotizarVentas oRespuestaCotizarVentas, EntVentaActualCotizar[] lstEntVentaActualCotizar)
        {
            if (oRespuestaCotizarVentas.ListaVentaActual != null && oRespuestaCotizarVentas.ListaVentaActual.Length > 0)
            {
                for (int i = 0; i < oRespuestaCotizarVentas.ListaVentaActual[0].ListaDetalleVenta.Length; i++)
                {
                    for (int j = 0; j < lstEntVentaActualCotizar[0].ListaDetalleVenta.Length; j++)
                    {
                        if (lstEntVentaActualCotizar[0].ListaDetalleVenta[j].SKU == oRespuestaCotizarVentas.ListaVentaActual[0].ListaDetalleVenta[i].SKU)
                        {
                            oRespuestaCotizarVentas.ListaVentaActual[0].ListaDetalleVenta[i].lstOmitirPromociones = lstEntVentaActualCotizar[0].ListaDetalleVenta[j].lstOmitirPromociones;
                            break;
                        }

                    }
                }
            }
        }

        public EntRespuestaPresupuestoNST GenerarPresupuesto(List<EntDetalleVentaBaseNST> lstEntDetalleVentaBaseNST, decimal montoTotalVenta, string ws, string idUsuario, string idSesion, EntVentaEmpleadoNST oEntVentaEmpleadoNST, EntDatosVentaApartado oEntDatosVentaApartado, EntClienteNST oEntClienteNST, EntComplementosNST oEntComplementosNST)
        {
            WCFServicioTienda wsWCFServicioTienda = new WCFServicioTienda();
            ResultadoVtaUniticket oResultadoVtaUniticket = new ResultadoVtaUniticket();
            EntRespuestaPresupuestoNST oEntRespuestaPresupuestoNST = new EntRespuestaPresupuestoNST();
            EntMarcadoVentaActual[] lstEntMarcadoVentaActual;
            ClienteIpadBase oClienteIpadBase = new ClienteIpadBase();
            try
            {
                this.lstDetalleVentaBonoAux = this.CrearDetalleVentaBono(lstEntDetalleVentaBaseNST, true);
                this.CrearDetalleSeguroVidaMax(lstEntDetalleVentaBaseNST, 0, false);
                oClienteIpadBase = this.CrearClienteIpadBase(oEntClienteNST, null, oEntComplementosNST);
                lstEntMarcadoVentaActual = this.CreaLstEntMarcadoVentaActual(lstEntDetalleVentaBaseNST, montoTotalVenta, oEntDatosVentaApartado, oEntComplementosNST);
                InformacionEmpleado oInformacionEmpleado = this.CrearInformacionEmpleado(oEntVentaEmpleadoNST);
                wsWCFServicioTienda.Timeout = 600000;
                oResultadoVtaUniticket = wsWCFServicioTienda.GenerarVtaUniticket(lstEntMarcadoVentaActual, oClienteIpadBase, idSesion, idUsuario, ws, this.oEntSeguroIpad, new Atributo[0], oInformacionEmpleado);
                oEntRespuestaPresupuestoNST = this.CreaRespuestaGenerarPresupuesto(oResultadoVtaUniticket);
            }
            catch (Exception ex)
            {
                oEntRespuestaPresupuestoNST.oEntRespuestaNST.eTipoError = EnumTipoErrorNST.Error;
                oEntRespuestaPresupuestoNST.oEntRespuestaNST.mensajeError = ex.Message;
            }
            return oEntRespuestaPresupuestoNST;
        }

        private bool AplicaVentaContado(PromocionAplicadaBase[] lstPromociones)
        {
            EntCatalogos catalogo = new EntCatalogos();
            DataSet dsParametro = catalogo.ObtenerCatalogoGenericoMaestro(1532, 18);

            try
            {
                if (dsParametro != null && dsParametro.Tables != null && dsParametro.Tables.Count > 0 && dsParametro.Tables[0].Rows.Count > 0)
                {
                    string cadenaPromos = dsParametro.Tables[0].Rows[0]["fcCatDesc"].ToString().Trim();

                    if (cadenaPromos.Trim().IndexOf(":") >= 0)
                        cadenaPromos = cadenaPromos.Trim().Substring(cadenaPromos.Trim().IndexOf(":") + 1);

                    string[] lstPromoID = cadenaPromos.Split(',');

                    foreach (string idProm in lstPromoID)
                    {
                        foreach (PromocionAplicadaBase PromApli in lstPromociones)
                        {
                            if (int.Parse(idProm.Trim()) == PromApli.PromocionId)
                                return true;
                        }
                    }
                }
                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine("Error al consultar catalogo 1532 subitem 18. " + " Mensaje: " + ex.Message + " Trace: " + ex.StackTrace, "LOG");
                return false;
            }
        }

        public EntRespuestaPresupuestoNST GenerarPresupuestoCredito(EntClienteNST oEntClienteNST, EntInfoPlazoNST oEntInfoPlazoNST, List<EntDetalleVentaBaseNST> lstEntDetalleVentaBaseNST, EntAccionesCreditoNST oEntAccionesCreditoNST, decimal montoTotalVenta, string ws, string idUsuario, string idSesion, EntVentaEmpleadoNST oEntVentaEmpleadoNST, EntComplementosNST oEntComplementosNST)
        {
            var metodo = this.GetType().Name + "." + System.Reflection.MethodBase.GetCurrentMethod().Name;
            _logs.AppendLine(metodo, "Inicia");
            _logs.AppendLineJson(metodo, "lstEntDetalleVentaBaseNST", lstEntDetalleVentaBaseNST);
            System.Diagnostics.Trace.WriteLine("Inicia GenerarPresupuestoCredito. NegocioNew", "LOG");
            WCFServicioTienda wsWCFServicioTienda = new WCFServicioTienda();
            ResultadoVtaUniticket oResultadoVtaUniticket = new ResultadoVtaUniticket();
            EntRespuestaPresupuestoNST oEntRespuestaPresupuestoNST = new EntRespuestaPresupuestoNST();
            EntMarcadoVentaActual[] lstEntMarcadoVentaActual;
            ClienteIpadBase oClienteIpadBase = new ClienteIpadBase();
            try
            {
                this.lstDetalleVentaBonoAux = this.CrearDetalleVentaBono(lstEntDetalleVentaBaseNST, true);
                this.CrearDetalleSeguroVidaMax(lstEntDetalleVentaBaseNST, oEntInfoPlazoNST.plazo, true);
                lstEntMarcadoVentaActual = this.CreaLstEntMarcadoVentaActual(oEntInfoPlazoNST, lstEntDetalleVentaBaseNST, oEntAccionesCreditoNST, montoTotalVenta, EnumTipoVenta.credito, null, oEntComplementosNST);
                oClienteIpadBase = this.CrearClienteIpadBase(oEntClienteNST, oEntAccionesCreditoNST, oEntComplementosNST);
                InformacionEmpleado oInformacionEmpleado = this.CrearInformacionEmpleado(oEntVentaEmpleadoNST);
                wsWCFServicioTienda.Timeout = 600000;
                if (oEntComplementosNST != null && oEntComplementosNST.oEntRecompensasRecomendar != null && oEntComplementosNST.oEntRecompensasRecomendar.EsAplicar && oEntComplementosNST.oEntRecompensasRecomendar.MontoCompra != "0" && oEntComplementosNST.oEntRecompensasRecomendar.DescuentoOtorgado != "0" && !String.IsNullOrEmpty(oEntComplementosNST.oEntRecompensasRecomendar.ClienteUnico))
                {
                    EntDescuentoRespuesta validaDescuentoRecompensa = ValidaDescuentoClienteRecomendado(new EntDescuentoPeticion()
                    {
                        clienteUnico = new EntClienteUnicoRecompensa()
                        {
                            pais = oEntComplementosNST.oEntRecompensasRecomendar.ClienteUnico.Split('-')[0],
                            canal = oEntComplementosNST.oEntRecompensasRecomendar.ClienteUnico.Split('-')[1],
                            sucursal = oEntComplementosNST.oEntRecompensasRecomendar.ClienteUnico.Split('-')[2],
                            folio = oEntComplementosNST.oEntRecompensasRecomendar.ClienteUnico.Split('-')[3]
                        },
                        descuentoOtorgado = oEntComplementosNST.oEntRecompensasRecomendar.DescuentoOtorgado,
                        montoCompra = oEntComplementosNST.oEntRecompensasRecomendar.MontoCompra
                    });
                    if (validaDescuentoRecompensa.error || validaDescuentoRecompensa.codigo > 0)
                    {
                        throw new Exception(validaDescuentoRecompensa.descripcion);
                    }
                }
                System.Diagnostics.Trace.WriteLine("Antes de wsWCFServicioTienda.GenerarVtaUniticket", "LOG");
                oResultadoVtaUniticket = wsWCFServicioTienda.GenerarVtaUniticket(lstEntMarcadoVentaActual, oClienteIpadBase, idSesion, idUsuario, ws, this.oEntSeguroIpad, new Atributo[0], oInformacionEmpleado);
                System.Diagnostics.Trace.WriteLine("Despues de wsWCFServicioTienda.GenerarVtaUniticket", "LOG");
                oEntRespuestaPresupuestoNST = this.CreaRespuestaGenerarPresupuesto(oResultadoVtaUniticket);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine("Error en NegocioNew. GenerarPresupuestoCredito." + " Mensaje: " + ex.Message + " Trace: " + ex.StackTrace, "LOG");
                _logs.EscribeLog();
                oEntRespuestaPresupuestoNST.oEntRespuestaNST.eTipoError = EnumTipoErrorNST.Error;
                oEntRespuestaPresupuestoNST.oEntRespuestaNST.mensajeError = ex.Message;
            }
            return oEntRespuestaPresupuestoNST;
        }

        public List<EntRespuestaPresupuestoNST> ObtenerPresupuestoRelacionado(int presupuesto)
        {
            WCFServicioTienda wsWCFServicioTienda = new WCFServicioTienda();
            EntRespuestaPresupuestoNST oEntRespuestaPresupuestoNST = new EntRespuestaPresupuestoNST();
            List<EntRespuestaPresupuestoNST> lstRes = new List<EntRespuestaPresupuestoNST>();
            ResultadoVtaSeparada oResultadoPres = new ResultadoVtaSeparada();
            try
            {
                oResultadoPres = wsWCFServicioTienda.ObtenerPresupuestoRelacionado(presupuesto, true);
                if (oResultadoPres.TipoRespuesta == EnumTipoError.SinError)
                {
                    foreach (int Pres in oResultadoPres.IdPresupuesto)
                    {
                        oEntRespuestaPresupuestoNST = new EntRespuestaPresupuestoNST();
                        oEntRespuestaPresupuestoNST.idPresupuesto = (long)Pres;
                        lstRes.Add(oEntRespuestaPresupuestoNST);
                    }
                }
                else
                {
                    oEntRespuestaPresupuestoNST.oEntRespuestaNST.mensajeError = oResultadoPres.Mensaje;
                    lstRes.Add(oEntRespuestaPresupuestoNST);
                }
            }
            catch (Exception ex)
            {
                lstRes = new List<EntRespuestaPresupuestoNST>();
                oEntRespuestaPresupuestoNST.oEntRespuestaNST.eTipoError = EnumTipoErrorNST.Error;
                oEntRespuestaPresupuestoNST.oEntRespuestaNST.mensajeError = ex.Message;
                lstRes.Add(oEntRespuestaPresupuestoNST);
            }
            return lstRes;
        }

        public EntRespuestaVentaNST GenerarVenta(EntContratoVentaNST oEntContratoVentaNST)
        {
            WCFServicioTienda wsWCFServicioTienda = new WCFServicioTienda();
            ResultadoVtaUniticket oResultadoVtaUniticket = new ResultadoVtaUniticket();
            EntRespuestaVentaNST oEntRespuestaVentaNST = new EntRespuestaVentaNST();
            EntMarcadoVentaActualCaja[] lstEntMarcadoVentaActual;
            ClienteIpadBase oClienteIpadBase = new ClienteIpadBase();
            try
            {
                oClienteIpadBase = this.CrearClienteIpadBase(oEntContratoVentaNST.oEntClienteNST, null, null);

                if (oClienteIpadBase.IdClienteCE > 0 || oClienteIpadBase.FolioCU > 0)
                    lstEntMarcadoVentaActual = this.CreaLstEntMarcadoVentaActualCaja(null, oEntContratoVentaNST.lstEntDetalleVentaBaseNST, null, oEntContratoVentaNST.montoTotalVenta, oEntContratoVentaNST.montoTotalEfectivo, EnumTipoVenta.contado, oEntContratoVentaNST.lstEntDocumentoProxyNST, null);
                else
                    lstEntMarcadoVentaActual = this.CreaLstEntMarcadoVentaActualCaja(oEntContratoVentaNST.lstEntDetalleVentaBaseNST, oEntContratoVentaNST.montoTotalVenta, oEntContratoVentaNST.montoTotalEfectivo, oEntContratoVentaNST.lstEntDocumentoProxyNST, null);

                oResultadoVtaUniticket = wsWCFServicioTienda.GenerarMarcarySurtirVtaUniticketCaja(lstEntMarcadoVentaActual, oClienteIpadBase, oEntContratoVentaNST.idSesion, oEntContratoVentaNST.idUsuario, oEntContratoVentaNST.ws, new EntSeguroIpad(), new Atributo[0]);
                oEntRespuestaVentaNST = this.CreaRespuestaGenerarMarcarySurtir(oResultadoVtaUniticket);

                if (!this.esTelefonia && oEntRespuestaVentaNST.idPedido > 0 && oEntContratoVentaNST.esSurtimiento)
                    this.AplicarSurtimiento(oEntRespuestaVentaNST, oEntContratoVentaNST.lstEntDetalleVentaBaseNST, oEntContratoVentaNST.idUsuario, oEntContratoVentaNST.ws);
            }
            catch (Exception ex)
            {
                oEntRespuestaVentaNST.oEntRespuestaNST.eTipoError = EnumTipoErrorNST.Error;
                oEntRespuestaVentaNST.oEntRespuestaNST.mensajeError = ex.Message;
            }
            return oEntRespuestaVentaNST;
        }

        public EntRespuestaVentaNST GenerarVentaEktCom(EntContratoVentaNST oEntContratoVentaNST)
        {
            WCFServicioTienda wsWCFServicioTienda = new WCFServicioTienda();
            ResultadoVtaUniticket oResultadoVtaUniticket = new ResultadoVtaUniticket();
            EntRespuestaVentaNST oEntRespuestaVentaNST = new EntRespuestaVentaNST();
            EntMarcadoVentaActualCaja[] lstEntMarcadoVentaActual;
            ClienteIpadBase oClienteIpadBase = new ClienteIpadBase();
            EntAccionesCreditoNST oEntAccionesCreditoNST = new EntAccionesCreditoNST();
            try
            {
                oClienteIpadBase = this.CrearClienteIpadBase(oEntContratoVentaNST.oEntClienteNST, null, null);
                oEntAccionesCreditoNST.idSecionCaja = "TOKEN|PAGOREALIZADO";

                lstEntMarcadoVentaActual = this.CreaLstEntMarcadoVentaActualCaja(oEntContratoVentaNST.oEntInfoPlazoNST, oEntContratoVentaNST.lstEntDetalleVentaBaseNST, oEntAccionesCreditoNST, oEntContratoVentaNST.montoTotalVenta, oEntContratoVentaNST.montoTotalEfectivo, EnumTipoVenta.contado, oEntContratoVentaNST.lstEntDocumentoProxyNST, null);

                oResultadoVtaUniticket = wsWCFServicioTienda.GenerarMarcarySurtirVtaUniticketCaja(lstEntMarcadoVentaActual, oClienteIpadBase, oEntContratoVentaNST.idSesion, oEntContratoVentaNST.idUsuario, oEntContratoVentaNST.ws, new EntSeguroIpad(), new Atributo[0]);
                oEntRespuestaVentaNST = this.CreaRespuestaGenerarMarcarySurtir(oResultadoVtaUniticket);

                if (!this.esTelefonia && oEntRespuestaVentaNST.idPedido > 0 && oEntContratoVentaNST.esSurtimiento)
                    this.AplicarSurtimiento(oEntRespuestaVentaNST, oEntContratoVentaNST.lstEntDetalleVentaBaseNST, oEntContratoVentaNST.idUsuario, oEntContratoVentaNST.ws);
            }
            catch (Exception ex)
            {
                oEntRespuestaVentaNST.oEntRespuestaNST.eTipoError = EnumTipoErrorNST.Error;
                oEntRespuestaVentaNST.oEntRespuestaNST.mensajeError = ex.Message;
            }
            return oEntRespuestaVentaNST;
        }

        public EntRespuestaPromocionesAlasNST ObtenerPromocionesAlas(EnumTipoVentaNST eTipoVentaNST)
        {
            WCFServicioTienda wsWCFServicioTienda = new WCFServicioTienda();
            EntRespuestaPromocionesAlasNST oEntRespuestaPromocionesAlasNST = new EntRespuestaPromocionesAlasNST();
            try
            {
                EnumTipoVenta tipoVenta = EnumTipoVenta.contado;
                if (eTipoVentaNST == EnumTipoVentaNST.Credito)
                    tipoVenta = EnumTipoVenta.credito;

                Atributo[] promociones = wsWCFServicioTienda.ObtienePromocionesEspeciales(tipoVenta, true);

                if (promociones.Length > 0)
                {
                    for (int i = 0; i < promociones.Length; i++)
                    {
                        EntPromocionesAlasNST oEntPromocionesAlasNST = new EntPromocionesAlasNST();
                        oEntPromocionesAlasNST.nombrePromocion = promociones[i].Value;
                        oEntPromocionesAlasNST.promocionId = Convert.ToInt32(promociones[i].Key);

                        oEntRespuestaPromocionesAlasNST.lstEntPromocionesAlasNST.Add(oEntPromocionesAlasNST);
                    }
                }
                else
                {
                    oEntRespuestaPromocionesAlasNST.oEntRespuestaNST.eTipoError = EnumTipoErrorNST.Warning;
                    oEntRespuestaPromocionesAlasNST.oEntRespuestaNST.mensajeError = "No hay promociones para mostrar";
                }
            }
            catch
            {
                oEntRespuestaPromocionesAlasNST.oEntRespuestaNST.eTipoError = EnumTipoErrorNST.Error;
                oEntRespuestaPromocionesAlasNST.oEntRespuestaNST.mensajeError = "Ocurrió un problema al consultar las promociones";
            }
            return oEntRespuestaPromocionesAlasNST;
        }

        public EntRespuestaProductosPromocion ObtenerProductosParaPromocion(List<EntDetalleVentaResNST> lstEntDetalleVentaResNST, int numeroRegistros, int numeroPagina, decimal montoVenta, int plazoSeleccionado)
        {
            WCFServicioTienda wsWCFServicioTienda = new WCFServicioTienda();
            EntRespuestaProductosPromocion oEntRespuestaProductosPromocion = new EntRespuestaProductosPromocion();
            string lstSkus = string.Empty;
            EnumTipoPromocion eTipoPromocion = EnumTipoPromocion.SinPromocion;
            int idPromocion = 0;
            decimal montoBono = 0;

            try
            {
                for (int i = 0; i < lstEntDetalleVentaResNST.Count; i++)
                {
                    lstSkus = lstEntDetalleVentaResNST[i].SKU.ToString();
                    for (int j = 0; j < lstEntDetalleVentaResNST[i].lstEntPromocionAplicadaNST.Count; j++)
                    {
                        if (lstEntDetalleVentaResNST[i].lstEntPromocionAplicadaNST[j].eTipoPromocion == EnumTipoPromocionNST.Elektrapesos)
                        {
                            eTipoPromocion = EnumTipoPromocion.Elektrapesos;
                            idPromocion = lstEntDetalleVentaResNST[i].lstEntPromocionAplicadaNST[j].promocionId;
                            montoBono += lstEntDetalleVentaResNST[i].lstEntPromocionAplicadaNST[j].montoOtorgado;
                        }

                        if (lstEntDetalleVentaResNST[i].lstEntPromocionAplicadaNST[j].eTipoPromocion == EnumTipoPromocionNST.MitayMita)
                        {
                            eTipoPromocion = EnumTipoPromocion.MitayMita;
                            idPromocion = lstEntDetalleVentaResNST[i].lstEntPromocionAplicadaNST[j].promocionId;
                            montoBono += lstEntDetalleVentaResNST[i].lstEntPromocionAplicadaNST[j].montoOtorgado;
                        }
                    }
                }

                if (eTipoPromocion == EnumTipoPromocion.Elektrapesos || eTipoPromocion == EnumTipoPromocion.MitayMita)
                {
                    DetalleVentaRes[] lstDetalleVentaRes = wsWCFServicioTienda.ObtenerProductosParaPromocion(eTipoPromocion, true, idPromocion, true, numeroRegistros, true,
                                                                                           numeroPagina, true, Convert.ToDouble(montoVenta), true, plazoSeleccionado, true,
                                                                                           lstSkus);
                    if (lstDetalleVentaRes.Length > 0)
                    {
                        for (int i = 0; i < lstDetalleVentaRes.Length; i++)
                        {
                            EntDetalleProductoBaseDNST oEntDetalleProductoBaseDNST = new EntDetalleProductoBaseDNST();
                            oEntDetalleProductoBaseDNST.Cantidad = 1;
                            oEntDetalleProductoBaseDNST.descripcion = lstDetalleVentaRes[i].Descripcion;
                            oEntDetalleProductoBaseDNST.precioLista = lstDetalleVentaRes[i].PrecioLista;
                            oEntDetalleProductoBaseDNST.SKU = lstDetalleVentaRes[i].SKU;
                            oEntRespuestaProductosPromocion.lstEntDetalleProductoBaseDNST.Add(oEntDetalleProductoBaseDNST);
                        }
                        for (int det = 0; det < lstEntDetalleVentaResNST.Count; det++)
                        {
                            if ((lstEntDetalleVentaResNST[det].eTipoAgregadoNST == EnumTipoAgregadoNST.PromocionBono ||
                                   lstEntDetalleVentaResNST[det].eTipoAgregadoNST == EnumTipoAgregadoNST.PromocionMita))
                            {
                                if (montoBono > 0)
                                {
                                    montoBono -= lstEntDetalleVentaResNST[det].precioLista;
                                    for (int bus = 0; bus < oEntRespuestaProductosPromocion.lstEntDetalleProductoBaseDNST.Count; bus++)
                                    {
                                        if (lstEntDetalleVentaResNST[det].SKU == oEntRespuestaProductosPromocion.lstEntDetalleProductoBaseDNST[bus].SKU)
                                        {
                                            lstEntDetalleVentaResNST.Remove(lstEntDetalleVentaResNST[det]);
                                            det--;
                                            break;
                                        }
                                    }
                                }
                            }
                            else
                            {
                                lstEntDetalleVentaResNST.Remove(lstEntDetalleVentaResNST[det]);
                                det--;
                            }
                        }
                        if (lstEntDetalleVentaResNST.Count > 0)
                        {
                            for (int det = 0; det < lstEntDetalleVentaResNST.Count; det++)
                                oEntRespuestaProductosPromocion.cadenaNOParticipantes += lstEntDetalleVentaResNST[det].SKU.ToString() + ",";
                        }
                    }
                    else
                    {
                        oEntRespuestaProductosPromocion.oEntRespuestaNST.eTipoError = EnumTipoErrorNST.Warning;
                        oEntRespuestaProductosPromocion.oEntRespuestaNST.mensajeError = "No hay productos disponibles para el canje";
                    }
                }
                else
                {
                    oEntRespuestaProductosPromocion.oEntRespuestaNST.eTipoError = EnumTipoErrorNST.Warning;
                    oEntRespuestaProductosPromocion.oEntRespuestaNST.mensajeError = "La venta no participa en la promoción";
                }
            }
            catch
            {
                oEntRespuestaProductosPromocion.oEntRespuestaNST.eTipoError = EnumTipoErrorNST.Error;
                oEntRespuestaProductosPromocion.oEntRespuestaNST.mensajeError = "Ocurrió un problema al consultar la lista de productos";
            }
            return oEntRespuestaProductosPromocion;
        }

        public EntRespuestaFacturacionNST FacturacionEPOS(EntPeticionFacturaNST oEntPeticionFacturaNST)
        {
            EntRespuestaFacturacionNST oEntRespuestaFacturacionNST = new EntRespuestaFacturacionNST();
            ContenedorFacturas oContenedorFacturas = new ContenedorFacturas();
            DatosFacturacion oDatosFacturacion = new DatosFacturacion();
            DatosCliente oDatosCliente = new DatosCliente();
            try
            {
                oDatosFacturacion = this.CrearDatosFacturacion(oEntPeticionFacturaNST.esDesgloceIVA, oEntPeticionFacturaNST.oEntDatosEntradaNST, oEntPeticionFacturaNST.idPedido);
                oDatosCliente = this.CrearDatosClienteFacturacion(oEntPeticionFacturaNST.oEntClienteFacturaNST);
                oContenedorFacturas = new WSFacturaDigital().ObtenerFacturaDigital(oDatosFacturacion, oDatosCliente, EnumTecnologia.ComercioElectronico, true);
                if (oContenedorFacturas.MensajeError != string.Empty)
                {
                    oEntRespuestaFacturacionNST.oEntRespuestaNST.eTipoError = EnumTipoErrorNST.Warning;
                    oEntRespuestaFacturacionNST.oEntRespuestaNST.mensajeError = oContenedorFacturas.MensajeError;
                }
                else
                    oEntRespuestaFacturacionNST.lstFacturas = this.GenerarRespuestaFacturacion(oContenedorFacturas);
            }
            catch (Exception ex)
            {
                oEntRespuestaFacturacionNST.oEntRespuestaNST.eTipoError = EnumTipoErrorNST.Error;
                oEntRespuestaFacturacionNST.oEntRespuestaNST.mensajeError = "Error al generar la factura para el pedido " + oEntPeticionFacturaNST.idPedido + ": " + ex.Message;
            }
            return oEntRespuestaFacturacionNST;
        }

        /// <summary>
        /// Consulta el número de pedido en base al número de presupuesto.
        /// </summary>
        /// <param name="idPresupuesto"></param>
        /// <returns></returns>
        public int ConsultarPredidoPorPresupuesto(int idPresupuesto)
        {
            int noPedido = 0;
            EntVentaCredito consulta = new EntVentaCredito();
            try
            {

                noPedido = consulta.ConsultarPedidoPorPresupuesto(idPresupuesto);
            }
            catch
            {
                noPedido = 0;
            }

            return noPedido;
        }

        public EntPresupuestoResNST ConsultaPresupuesto(int tienda, int idPresupuesto)
        {
            EntPresupuestoResNST oEntPresupuestoResNST = new EntPresupuestoResNST();
            try
            {
                ServicioTienda oServicioTienda = new ServicioTienda();
                Tienda oTienda = oServicioTienda.Consultar(tienda, true);
                switch (oTienda.Estado)
                {
                    case 0:
                        oEntPresupuestoResNST.oEntRespuestaNST.eTipoError = EnumTipoErrorNST.Warning;
                        oEntPresupuestoResNST.oEntRespuestaNST.mensajeError = "La tienda está inactiva";
                        break;
                    case 1:
                        WCFServicioTienda oWCFServicioTienda = new WCFServicioTienda(oTienda.IP);
                        EntPresupuestoRes oEntPresupuestoRes = oWCFServicioTienda.ConsultaPresupuesto(tienda, true, idPresupuesto, true);
                        oEntPresupuestoResNST = this.GeneraEntPresupuestoResNST(oEntPresupuestoRes);
                        break;
                    case 2:
                        oEntPresupuestoResNST.oEntRespuestaNST.eTipoError = EnumTipoErrorNST.Warning;
                        oEntPresupuestoResNST.oEntRespuestaNST.mensajeError = "La tienda NO está registrada";
                        break;
                }
            }
            catch (Exception ex)
            {
                oEntPresupuestoResNST.oEntRespuestaNST.eTipoError = EnumTipoErrorNST.Error;
                oEntPresupuestoResNST.oEntRespuestaNST.mensajeError = "Ocurrió un error al consultar los datos del presupuesto: " + idPresupuesto.ToString() + ", \n" + ex.Message;
            }
            return oEntPresupuestoResNST;
        }

        public EntRespuestaConProductoNST ConsultarDatosProductoTienda(int tienda, int sku)
        {
            EntRespuestaConProductoNST oEntRespuestaConProductoNST = new EntRespuestaConProductoNST();
            try
            {
                ServicioTienda oServicioTienda = new ServicioTienda();
                Tienda oTienda = oServicioTienda.Consultar(tienda, true);
                switch (oTienda.Estado)
                {
                    case 0:
                        oEntRespuestaConProductoNST.oEntRespuestaNST.eTipoError = EnumTipoErrorNST.Warning;
                        oEntRespuestaConProductoNST.oEntRespuestaNST.mensajeError = "La tienda está inactiva";
                        break;
                    case 1:
                        WCFServicioTienda oWCFServicioTienda = new WCFServicioTienda(oTienda.IP);
                        DetalleVentaRes[] lstDetalleVentaRes = oWCFServicioTienda.ConsultarDatosProductoTienda(tienda, true, sku, true);
                        oEntRespuestaConProductoNST = this.GeneraEntDetalleVentaResNST(lstDetalleVentaRes);
                        break;
                    case 2:
                        oEntRespuestaConProductoNST.oEntRespuestaNST.eTipoError = EnumTipoErrorNST.Warning;
                        oEntRespuestaConProductoNST.oEntRespuestaNST.mensajeError = "La tienda NO está registrada";
                        break;
                }
            }
            catch
            {
                oEntRespuestaConProductoNST.oEntRespuestaNST.eTipoError = EnumTipoErrorNST.Error;
                oEntRespuestaConProductoNST.oEntRespuestaNST.mensajeError = "ocurrió un error al consultar los datos del producto";
            }
            return oEntRespuestaConProductoNST;
        }

        public EntRespuestaVentaNST GeneraPedidoBAZDigital(int noTienda, int idPresupuesto, decimal montoPagar, string referenciaPres)
        {
            EntRespuestaVentaNST oEntRespuestaVentaNST = new EntRespuestaVentaNST();
            try
            {
                ServicioTienda oServicioTienda = new ServicioTienda();
                Tienda oTienda = oServicioTienda.Consultar(noTienda, true);
                switch (oTienda.Estado)
                {
                    case 0:
                        oEntRespuestaVentaNST.oEntRespuestaNST.eTipoError = EnumTipoErrorNST.Warning;
                        oEntRespuestaVentaNST.oEntRespuestaNST.mensajeError = "La tienda está inactiva";
                        break;
                    case 1:
                        WCFServicioTienda oWCFServicioTienda = new WCFServicioTienda(oTienda.IP);
                        ResultadoVtaUniticket oResultadoVtaUniticket = oWCFServicioTienda.GeneraPedidoBAZDigital(noTienda, true, idPresupuesto, true, montoPagar, true, referenciaPres);
                        oEntRespuestaVentaNST = this.CreaRespuestaGenerarMarcarySurtir(oResultadoVtaUniticket);
                        break;
                    case 2:
                        oEntRespuestaVentaNST.oEntRespuestaNST.eTipoError = EnumTipoErrorNST.Warning;
                        oEntRespuestaVentaNST.oEntRespuestaNST.mensajeError = "La tienda NO está registrada";
                        break;
                }
            }
            catch (Exception ex)
            {
                oEntRespuestaVentaNST.oEntRespuestaNST.eTipoError = EnumTipoErrorNST.Error;
                oEntRespuestaVentaNST.oEntRespuestaNST.mensajeError = "Error al generar el pedido: " + ex.Message;
            }
            return oEntRespuestaVentaNST;
        }

        public EntResAbono ConsultaAbonoAdicional(string CadenaProductos, decimal Enganche, int plazo, int Periodo, bool aplicaPromo)
        {
            EntAbono abono = null;
            EntResAbono resAbono = new EntResAbono();
            EntConsultasBDNST consultaBD = new EntConsultasBDNST();
            EntPromocionDefinicion manejadorPromocion = new EntPromocionDefinicion();
            DateTime dt = DateTime.Now;
            bool aplicaPrecio = false;
            EntCatalogos catalogo = new EntCatalogos();
            decimal montoMinimo = 0;
            int plazoMinimo = 0;
            string msjPromo = string.Empty;

            try
            {
                DataSet ds = catalogo.ObtenerCatalogoGenericoMaestro(1712);
                if (ds.Tables[0].Rows.Count > 0)
                    montoMinimo = Convert.ToDecimal(ds.Tables[0].Rows[0]["fcCatDesc"].ToString());
                if (ds.Tables[0].Rows.Count > 1)
                    plazoMinimo = Convert.ToInt16(ds.Tables[0].Rows[1]["fcCatDesc"].ToString());
                if (ds.Tables[0].Rows.Count > 0)
                    msjPromo = ds.Tables[0].Rows[2]["fcCatDesc"].ToString();


                if (CadenaProductos.Trim() != string.Empty && plazo > 0)
                {
                    DataSet dsRes = consultaBD.ObtenerAbonosCalulados(CadenaProductos, 0, plazo, Periodo, 0, aplicaPromo);

                    if (dsRes != null && dsRes.Tables != null && dsRes.Tables.Count > 0 && dsRes.Tables[0].Rows.Count > 0)
                    {
                        for (int i = 0; i < dsRes.Tables[0].Rows.Count; i++)
                        {
                            abono = new EntAbono();
                            abono.Abono = Convert.ToDecimal(dsRes.Tables[0].Rows[i]["fnAbono"].ToString());
                            abono.UAbono = Convert.ToDecimal(dsRes.Tables[0].Rows[i]["fnUAbono"].ToString());
                            abono.AbonoPP = Convert.ToDecimal(dsRes.Tables[0].Rows[i]["fnAbonoP"].ToString());
                            abono.TotalVta = Convert.ToDecimal(dsRes.Tables[0].Rows[i]["fnPrecioTotal"].ToString());
                            abono.SKU = int.Parse(dsRes.Tables[0].Rows[i]["fiProdid"].ToString());
                            resAbono.LstAbonos.Add(abono);

                            if (abono.SKU == 0 && abono.TotalVta >= montoMinimo && plazo >= plazoMinimo)
                            {
                                aplicaPrecio = true;
                            }
                        }
                    }


                    dsRes = null;
                    dsRes = consultaBD.ObtenerAbonosCalulados(CadenaProductos, Convert.ToInt16(Enganche), plazo, Periodo, 1, aplicaPromo);

                    if (dsRes != null && dsRes.Tables != null && dsRes.Tables.Count > 0 && dsRes.Tables[0].Rows.Count > 0)
                    {
                        for (int i = 0; i < dsRes.Tables[0].Rows.Count; i++)
                        {
                            abono = new EntAbono();
                            abono.Abono = Convert.ToDecimal(dsRes.Tables[0].Rows[i]["fnAbono"].ToString());
                            abono.UAbono = Convert.ToDecimal(dsRes.Tables[0].Rows[i]["fnUAbono"].ToString());
                            abono.AbonoPP = Convert.ToDecimal(dsRes.Tables[0].Rows[i]["fnAbonoP"].ToString());
                            abono.TotalVta = Convert.ToDecimal(dsRes.Tables[0].Rows[i]["fnPrecioTotal"].ToString());
                            abono.SKU = int.Parse(dsRes.Tables[0].Rows[i]["fiProdid"].ToString());
                            resAbono.LstAbonosNormal.Add(abono);
                        }
                    }

                    manejadorPromocion.BeginObject(2684);

                    if (dt >= manejadorPromocion.VigenciaInicial && dt <= manejadorPromocion.VigenciaFinal && aplicaPrecio && aplicaPromo)
                    {
                        resAbono.aplicaMecanicaPromocion = true;
                        resAbono.msjPromocionEnganche = msjPromo;
                    }
                    else
                    {
                        resAbono.aplicaMecanicaPromocion = false;
                        resAbono.msjPromocionEnganche = "";
                    }
                }
                else
                {
                    resAbono.oEntRespuestaNST.eTipoError = EnumTipoErrorNST.Error;
                    resAbono.oEntRespuestaNST.mensajeError = "No se cuenta con la información requerida para calcular los abonos.";
                }
            }
            catch (Exception ex)
            {
                resAbono.oEntRespuestaNST.eTipoError = EnumTipoErrorNST.Error;
                resAbono.oEntRespuestaNST.mensajeError = "Error al consultar los Abonos: " + ex.Message;
            }

            return resAbono;
        }


        public EntResAbono ConsultaAbonoAdicionalNuevosAbonos(string CadenaProductos, int plazo, int Periodo, int TipoCte)
        {
            System.Diagnostics.Trace.WriteLine("ConsultaAbonoAdicionalNuevosAbonos() --> CadenaProductos: " + CadenaProductos + " plazo: " + plazo.ToString() + " Periodo: " + Periodo.ToString() + " TipoCte: " + TipoCte.ToString(), "LOG");
            EntAbono abono = null;
            EntResAbono resAbono = new EntResAbono();
            EntConsultasBDNST consultaBD = new EntConsultasBDNST();
            DataSet dsRes = new DataSet();
            EntPlazosVenta ResPlazos = new EntPlazosVenta();
            EntPlazoAbono AbonosTotal = new EntPlazoAbono();
            decimal TotalPrec = 0;
            try
            {
                if (CadenaProductos.Trim() != string.Empty && plazo > 0)
                {
                    System.Diagnostics.Trace.WriteLine("Antes de ConsultaPlazosCalculadosVenta()", "LOG");
                    ResPlazos = this.ConsultaPlazosCalculadosVenta(CadenaProductos, 3, TipoCte, Periodo, 200, 0, "", plazo.ToString(), false);
                    System.Diagnostics.Trace.WriteLine("Despues de ConsultaPlazosCalculadosVenta() --> Cantidad: " + ResPlazos.LstPlazosSKU.Count.ToString(), "LOG");

                    string[] CadenaSKU = CadenaProductos.Trim().Split('|');

                    if (ResPlazos != null && ResPlazos.LstPlazosSKU != null && ResPlazos.LstPlazosSKU.Count > 0)
                    {
                        foreach (EntPlazosSKU lstSKU in ResPlazos.LstPlazosSKU)
                        {
                            foreach (EntPlazoAbono lstAbonos in lstSKU.lstEntPlazoAbono)
                            {
                                if (lstSKU.SKU == 1)
                                    AbonosTotal = lstAbonos;

                                foreach (string Valor in CadenaSKU)
                                {
                                    if (Valor.Trim().Length > 5)
                                    {
                                        string[] ValorCadena = Valor.Trim().Split(',');

                                        if (int.Parse(ValorCadena[0].Trim()) == lstSKU.SKU && lstSKU.SKU != 1)
                                        {
                                            abono = new EntAbono();
                                            abono.Abono = lstAbonos.abono;
                                            abono.UAbono = lstAbonos.ultimoAbono;
                                            abono.AbonoPP = lstAbonos.abonoPuntual;
                                            abono.TotalVta = (Convert.ToDecimal(ValorCadena[1].Trim()) * (Convert.ToDecimal(ValorCadena[2].Trim()) - Convert.ToDecimal(ValorCadena[3].Trim())));
                                            TotalPrec = TotalPrec + abono.TotalVta;
                                            abono.SKU = lstSKU.SKU;
                                            resAbono.LstAbonos.Add(abono);
                                        }
                                    }
                                }
                            }
                        }

                        if (resAbono.LstAbonos.Count > 0 && TotalPrec > 0)
                        {
                            abono = new EntAbono();
                            abono.Abono = AbonosTotal.abono;
                            abono.UAbono = AbonosTotal.ultimoAbono;
                            abono.AbonoPP = AbonosTotal.abonoPuntual;
                            abono.TotalVta = TotalPrec;
                            abono.SKU = 0;
                            resAbono.LstAbonos.Add(abono);
                        }

                    }
                    else
                    {
                        resAbono.oEntRespuestaNST.eTipoError = EnumTipoErrorNST.Error;
                        resAbono.oEntRespuestaNST.mensajeError = "No se pudieron recuperar abonos para los parametros indicados.";
                    }
                }
                else
                {
                    resAbono.oEntRespuestaNST.eTipoError = EnumTipoErrorNST.Error;
                    resAbono.oEntRespuestaNST.mensajeError = "No se cuenta con la información requerida para calcular los abonos.";
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine("Error en ConsultaAbonoAdicionalNuevosAbonos() --> MSJ: " + ex.Message + " StackTrace: " + ex.StackTrace, "LOG");
                resAbono.oEntRespuestaNST.eTipoError = EnumTipoErrorNST.Error;
                resAbono.oEntRespuestaNST.mensajeError = "Error al consultar los Abonos: " + ex.Message;
            }

            System.Diagnostics.Trace.WriteLine("Termina ConsultaAbonoAdicionalNuevosAbonos()", "LOG");
            return resAbono;
        }

        public EntRespuestaEstatusPedidoNST ConsultarEstatusPedido(int noTienda, int idFolio, EnumTipoFolioNST eTipoFolioNST)
        {
            EntRespuestaEstatusPedidoNST oEntRespuestaEstatusPedidoNST = new EntRespuestaEstatusPedidoNST();
            try
            {
                ServicioTienda oServicioTienda = new ServicioTienda();
                Tienda oTienda = oServicioTienda.Consultar(noTienda, true);
                switch (oTienda.Estado)
                {
                    case 0:
                        oEntRespuestaEstatusPedidoNST.oEntRespuestaNST.eTipoError = EnumTipoErrorNST.Warning;
                        oEntRespuestaEstatusPedidoNST.oEntRespuestaNST.mensajeError = "La tienda está inactiva";
                        break;
                    case 1:
                        WCFServicioTienda oWCFServicioTienda = new WCFServicioTienda(oTienda.IP);
                        EnumTipoFolio eTipoFolio = EnumTipoFolio.FolioUniticket;
                        switch (eTipoFolioNST)
                        {
                            case EnumTipoFolioNST.FolioPedido:
                                eTipoFolio = EnumTipoFolio.FolioPedido;
                                break;
                            case EnumTipoFolioNST.FolioPresupuesto:
                                eTipoFolio = EnumTipoFolio.FolioPresupuesto;
                                break;
                        }
                        EntRespuestaEstatusPedido oEntRespuestaEstatusPedido = oWCFServicioTienda.ConsultarEstatusPedido(idFolio, true, eTipoFolio, true);
                        oEntRespuestaEstatusPedidoNST = this.GeneraEntRespuestaEstatusPedidoNST(oEntRespuestaEstatusPedido);
                        break;
                    case 2:
                        oEntRespuestaEstatusPedidoNST.oEntRespuestaNST.eTipoError = EnumTipoErrorNST.Warning;
                        oEntRespuestaEstatusPedidoNST.oEntRespuestaNST.mensajeError = "La tienda NO está registrada";
                        break;
                }
            }
            catch (Exception ex)
            {
                oEntRespuestaEstatusPedidoNST.oEntRespuestaNST.eTipoError = EnumTipoErrorNST.Error;
                oEntRespuestaEstatusPedidoNST.oEntRespuestaNST.mensajeError = "Error al generar el pedido: " + ex.Message;
            }
            return oEntRespuestaEstatusPedidoNST;
        }

        public GuiasEmpleado.Entidades.EntResultado ConsultarGuias(string empleado)
        {
            return new OperacionesFamilia().buscaFamilias(empleado);
        }

        public GuiasEmpleado.Entidades.EntResultadoActualizacion ActualizarGuia(int consecutivo, string empleado, int status)
        {
            return new OperacionesFamilia().ActualizarFamilia(consecutivo, empleado, status);
        }

        public EntRutasFamilias ConsultaRutaPDFMarcaAgua(EntRutasFamilias p_RutasPDF)
        {
            return new OperacionesFamilia().ObtenerNuevasRutas(p_RutasPDF);
        }

        public EntResPres ConsultaFolioPresupuesto(int folio)
        {

            EntResPres ResPres = new EntResPres();

            try
            {
                EntConsultasBDNST consulta = new EntConsultasBDNST();
                DataSet dsCampos = consulta.ObtenerFolioPresupuestoCN(folio);

                if (dsCampos != null && dsCampos.Tables != null && dsCampos.Tables.Count > 0 && dsCampos.Tables[0].Rows.Count > 0)
                {
                    for (int i = 0; i < dsCampos.Tables[0].Rows.Count; i++)
                    {
                        EntPres Pres = new EntPres();
                        Pres.fiProdId = int.Parse(dsCampos.Tables[0].Rows[i]["fiProdId"].ToString());
                        Pres.fcProdDesc = dsCampos.Tables[0].Rows[i]["fcProdDesc"].ToString();
                        Pres.fiCantidad = int.Parse(dsCampos.Tables[0].Rows[i]["fiCantidad"].ToString());
                        Pres.fnProdPrecio = Convert.ToDecimal(dsCampos.Tables[0].Rows[i]["fnProdPrecio"].ToString());
                        Pres.fiDescuento = Convert.ToDecimal(dsCampos.Tables[0].Rows[i]["fiDescuento"].ToString());
                        Pres.fdFechaPres = Convert.ToDateTime(dsCampos.Tables[0].Rows[i]["fdFechaPres"].ToString());
                        Pres.fiPlazo = int.Parse(dsCampos.Tables[0].Rows[i]["fiPlazo"].ToString());
                        Pres.fnAbono = Convert.ToDecimal(dsCampos.Tables[0].Rows[i]["fnAbono"].ToString());
                        Pres.fnAbonoPuntual = Convert.ToDecimal(dsCampos.Tables[0].Rows[i]["fnAbonoPuntual"].ToString());
                        Pres.fnUltimoAbono = Convert.ToDecimal(dsCampos.Tables[0].Rows[i]["fnUltimoAbono"].ToString());
                        Pres.fnEnganche = Convert.ToDecimal(dsCampos.Tables[0].Rows[i]["fnEnganche"].ToString());
                        Pres.fnSobreprecio = Convert.ToDecimal(dsCampos.Tables[0].Rows[i]["fnSobreprecio"].ToString());
                        Pres.fnTotalPrecio = Convert.ToDecimal(dsCampos.Tables[0].Rows[i]["fnTotalPrecio"].ToString());
                        ResPres.LstPresupuesto.Add(Pres);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine("Error al consultar el parametro " + folio.ToString() + ". Msj: " + ex.Message + ". Trace: " + ex.StackTrace, "LOG");
            }
            return ResPres;
        }

        public EntRespuestaCAPConsultarTAZ CapConsultarTAZ(int PaisCteUnico, int CanalCteUnico, int SucursalCteUnico, int FolioCteUnico, string usuario, string password, string estacionTrabajo)
        {

            string strClienteUnico = string.Empty;
            string strUrl = string.Empty;
            int intSeMuestraConsultaTAZ = 1;

            strClienteUnico = PaisCteUnico.ToString("00") + CanalCteUnico.ToString("00") + SucursalCteUnico.ToString() + FolioCteUnico.ToString();

            NuevoCodigoSalida objRespuestaServiceCAPConsultarTAZ = new NuevoCodigoSalida();

            EntRespuestaCAPConsultarTAZ objRespuestaCAPConsultarTAZ = new EntRespuestaCAPConsultarTAZ();

            EntCatalogos catalogo = new EntCatalogos();

            intSeMuestraConsultaTAZ = Convert.ToInt32(catalogo.ObtenerParametroNegocio(173));

            if (intSeMuestraConsultaTAZ == 1)
            {
                try
                {
                    System.Diagnostics.Trace.WriteLine("Dentro de CapConsultarTAZ en ManejadorConsultaST de Negocio.NewServicioTienda, con los siguientes parametros", "log");

                    System.Diagnostics.Trace.WriteLine("PaisCteUnico: " + PaisCteUnico.ToString(), "log");
                    System.Diagnostics.Trace.WriteLine("CanalCteUnico: " + CanalCteUnico.ToString(), "log");
                    System.Diagnostics.Trace.WriteLine("SucursalCteUnico: " + SucursalCteUnico.ToString(), "log");
                    System.Diagnostics.Trace.WriteLine("FolioCteUnico: " + FolioCteUnico.ToString(), "log");
                    System.Diagnostics.Trace.WriteLine("usuario: " + usuario.ToString(), "log");
                    System.Diagnostics.Trace.WriteLine("password: " + password, "log");
                    System.Diagnostics.Trace.WriteLine("estacionTrabajo: " + estacionTrabajo.ToString(), "log");

                    strUrl = catalogo.ObtenerParametroNegocio(172);

                    Service1 objServiceCAPConsultarTAZ = new Service1();

                    objServiceCAPConsultarTAZ.Url = strUrl;

                    System.Diagnostics.Trace.WriteLine("Antes de Captación Consultar TAZ", "log");
                    objRespuestaServiceCAPConsultarTAZ = objServiceCAPConsultarTAZ.CallService1(strClienteUnico, usuario, password, estacionTrabajo);

                    System.Diagnostics.Trace.WriteLine("Después de Captación Consultar TAZ", "log");

                    if (objRespuestaServiceCAPConsultarTAZ.Id == 0)
                    {
                        System.Diagnostics.Trace.WriteLine("Exito en método Captación Consultar TAZ. Id = 0; Estatus = " + objRespuestaServiceCAPConsultarTAZ.Estatus.ToString() + "; PopUp1 = " + objRespuestaServiceCAPConsultarTAZ.PopUp1 + "; PopUp2 = " + objRespuestaServiceCAPConsultarTAZ.PopUp2, "log");
                        objRespuestaCAPConsultarTAZ.Id = 0;
                        objRespuestaCAPConsultarTAZ.Estatus = objRespuestaServiceCAPConsultarTAZ.Estatus;


                        string[] strMensajesSeparadosPopUp = objRespuestaServiceCAPConsultarTAZ.PopUp1.Split(new string[] { "|" }, StringSplitOptions.RemoveEmptyEntries);

                        objRespuestaCAPConsultarTAZ.PopUp1_1 = strMensajesSeparadosPopUp[0].ToString().Trim();
                        if (strMensajesSeparadosPopUp.Count() == 2)
                            objRespuestaCAPConsultarTAZ.PopUp1_2 = strMensajesSeparadosPopUp[1].ToString().Trim();

                        strMensajesSeparadosPopUp = objRespuestaServiceCAPConsultarTAZ.PopUp2.Split(new string[] { "|" }, StringSplitOptions.RemoveEmptyEntries);

                        objRespuestaCAPConsultarTAZ.PopUp2_1 = strMensajesSeparadosPopUp[0].ToString().Trim();
                        if (strMensajesSeparadosPopUp.Count() == 2)
                            objRespuestaCAPConsultarTAZ.PopUp2_2 = strMensajesSeparadosPopUp[1].ToString().Trim();

                    }
                    else
                    {
                        System.Diagnostics.Trace.WriteLine("Error en método Captación Consultar TAZ. Id = -1; Estatus = " + objRespuestaServiceCAPConsultarTAZ.Estatus.ToString(), "log");
                        objRespuestaCAPConsultarTAZ.Id = -1;
                        objRespuestaCAPConsultarTAZ.Estatus = objRespuestaServiceCAPConsultarTAZ.Estatus;
                        objRespuestaCAPConsultarTAZ.PopUp1_1 = "";
                        objRespuestaCAPConsultarTAZ.PopUp2_1 = "";
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Trace.WriteLine("Excepción en método Captación Consultar TAZ: " + ex.Message, "log");
                    objRespuestaCAPConsultarTAZ.Id = -1;
                    objRespuestaCAPConsultarTAZ.Estatus = -1;
                    objRespuestaCAPConsultarTAZ.PopUp1_1 = "";
                    objRespuestaCAPConsultarTAZ.PopUp2_1 = "";
                }

                System.Diagnostics.Trace.WriteLine("Fin de CapConsultarTAZ en NegocioSurtimientoBase", "log");

                return objRespuestaCAPConsultarTAZ;
            }
            else
            {
                System.Diagnostics.Trace.WriteLine("Se omitió la consulta TAZ", "log");
                objRespuestaCAPConsultarTAZ.Id = 0;
                objRespuestaCAPConsultarTAZ.Estatus = 0;
                objRespuestaCAPConsultarTAZ.PopUp1_1 = "";
                objRespuestaCAPConsultarTAZ.PopUp2_1 = "";

                return objRespuestaCAPConsultarTAZ;
            }
        }

        /*  public EntRespuestaInvCentral ConsultaInventarioCentral(string Ubicaciones, string CadenaSKU)
          {
              string [] SplitUbi = {};
              string [] SplitProd = {};
              EntInventarioProducto resInv = new EntInventarioProducto();
              EntRespuestaInvCentral RespInv = new EntRespuestaInvCentral();
              InventarioService conInv = new InventarioService();
              ConsInv.Request RequestInv = new ConsInv.Request();
              ConsInv.Producto SKU = new ConsInv.Producto();
              List<ConsInv.Producto> listProd = new List<ConsInv.Producto>();
              List<ConsInv.Producto> ResultlistProd = new List<ConsInv.Producto>();
              EntTienda tienda = new EntTienda();
              int NoTienda = 0;


              try
              {
                  System.Diagnostics.Trace.WriteLine("*** Inicia consulta Inventario central *** Tienda: " + NoTienda.ToString() + " Ubicaciones: " + Ubicaciones + " CadenaSKU: " + CadenaSKU, "LOG");

                  DataSet dsTienda = tienda.ConsultaDatosControl();
                  if (dsTienda != null && dsTienda.Tables != null && dsTienda.Tables.Count > 0 && dsTienda.Tables[0].Rows.Count > 0)
                      NoTienda = int.Parse(dsTienda.Tables[0].Rows[0]["fiNoTienda"].ToString());

                  SplitUbi = Ubicaciones.Trim().Split(',');
                  SplitProd = CadenaSKU.Trim().Split(',');

                  foreach (string prod in SplitProd)
                  {
                      SKU.idProducto = int.Parse(prod);
                      listProd.Add(SKU);
                  }

                  RequestInv.tienda = NoTienda;
                  RequestInv.ubicacion = 0;
                  RequestInv.productos = listProd;


                  System.Diagnostics.Trace.WriteLine("*** Envio a servicio ***", "LOG");
                  ConsInv.Response RespuestaInv = conInv.consultarInventario(RequestInv);
                  System.Diagnostics.Trace.WriteLine("*** Respuesta servicio ***", "LOG");

                  ResultlistProd = RespuestaInv.sdf.free.respuesta.resultado.respuesta.detalleRespuesta.productos;

                  System.Diagnostics.Trace.WriteLine("*** ResultlistProd.Count: " + ResultlistProd.Count.ToString(), "LOG");

                  foreach (ConsInv.Producto respProd in ResultlistProd)
                  {
                      int cantidad = 0;
                      resInv.SKU = respProd.idProducto;

                      foreach (string ubicacion in SplitUbi)
                      {
                          foreach (ConsInv.Ubicacion ubi in respProd.ubicaciones)
                          {
                              if (ubi.ubicacion == int.Parse(ubicacion))
                              {
                                  cantidad = cantidad + ubi.existencia;
                                  break;
                              }
                          }
                      }
                      resInv.Cantidad = cantidad;
                      RespInv.LstProductoInv.Add(resInv);
                  }
              }
              catch (Exception ex)
              {
                  System.Diagnostics.Trace.WriteLine("*** Ocurrio un error al consultar el inventario central ***", "LOG");
                  System.Diagnostics.Trace.WriteLine("Error: " + ex.Message + " StackTrace: " + ex.StackTrace, "LOG");
                  RespInv.LstProductoInv = new List<EntInventarioProducto>();
                  RespInv.oEntRespuesta.eTipoError = EnumTipoErrorNST.Error;
                  RespInv.oEntRespuesta.mensajeError = "Error al consultar inventario: " + ex.Message;
              }

              return RespInv;
          }

          public void AfectaInventarioCanjeCentral(ArrayList arregloInventarios, string ws, string Empleado, string Ref, IDbTransaction transaccionBaseDatos, Transactions transaccionNegocio)
          {
              try
              {
                  System.Diagnostics.Trace.WriteLine("*** Inicia Descarga de inventario del regalo ***", "LOG");

                  InventarioService invCentral = new InventarioService();
                  InventarioCentral.DTO.Salida.MovimientoInventario Movimiento = new InventarioCentral.DTO.Salida.MovimientoInventario();
                  //Movimiento.

                  invCentral.ActualizaInventario(arregloInventarios,ws,Empleado,Ref,transaccionBaseDatos,transaccionNegocio,Movimiento);

                  System.Diagnostics.Trace.WriteLine("*** Termina Descarga de inventario del regalo ***", "LOG");
              }
              catch (Exception ex)
              {
                  System.Diagnostics.Trace.WriteLine("*** Ocurrio un error al Descargar el inventario del regalo ***", "LOG");
                  System.Diagnostics.Trace.WriteLine("Error: " + ex.Message + " StackTrace: " + ex.StackTrace, "LOG");
              }
          }*/

        public string ConsultaPagosLigeros(string JsonPagosLigeros)
        {
            string RespuestaJSON = null;
            try
            {
                string URL = string.Empty, User = string.Empty, pwd = string.Empty, myIP = string.Empty;
                System.Diagnostics.Trace.WriteLine("Inicia ConsultaPagosLigeros.", "log");
                ManejadorConsultaWS consWS = new ManejadorConsultaWS();
                EntCatalogos catalogo = new EntCatalogos();
                DataSet dsURL = catalogo.ObtenerCatalogoGenericoMaestro(1532, 20);
                DataSet dsUser = catalogo.ObtenerCatalogoGenericoMaestro(1532, 21);
                DataSet dsPwd = catalogo.ObtenerCatalogoGenericoMaestro(1532, 22);
                DataSet dsIP = catalogo.ObtenerCatalogoGenericoMaestro(1532, 23);
                //string myHost = System.Net.Dns.GetHostName();
                //string myIP = System.Net.Dns.GetHostEntry(myHost).AddressList[0].ToString();

                if (dsURL != null && dsURL.Tables != null && dsURL.Tables.Count > 0 && dsURL.Tables[0].Rows.Count > 0)
                    URL = dsURL.Tables[0].Rows[0]["fcCatDesc"].ToString().Trim();

                if (dsUser != null && dsUser.Tables != null && dsUser.Tables.Count > 0 && dsUser.Tables[0].Rows.Count > 0)
                    User = dsUser.Tables[0].Rows[0]["fcCatDesc"].ToString().Trim();

                if (dsPwd != null && dsPwd.Tables != null && dsPwd.Tables.Count > 0 && dsPwd.Tables[0].Rows.Count > 0)
                    pwd = dsPwd.Tables[0].Rows[0]["fcCatDesc"].ToString().Trim();

                if (dsIP != null && dsIP.Tables != null && dsIP.Tables.Count > 0 && dsIP.Tables[0].Rows.Count > 0)
                    myIP = dsIP.Tables[0].Rows[0]["fcCatDesc"].ToString().Trim();

                if ((URL == null || URL == string.Empty) || (User == null || User == string.Empty) || (pwd == null || pwd == string.Empty))
                {
                    System.Diagnostics.Trace.WriteLine("No fue posible recuperar lainformación de Catalogo_Generico 1532 subItem 20,21,22.", "log");
                    return null;
                }

                System.Diagnostics.Trace.WriteLine("user: " + User + " password: " + pwd + " ip: " + myIP + " URL: " + URL, "log");

                //ServicePointManager.SecurityProtocol = ((SecurityProtocolType)System.Net.EPOS.EKT.SecurityProtocolType.Ssl3) | ((SecurityProtocolType)System.Net.EPOS.EKT.SecurityProtocolType.Tls) | ((SecurityProtocolType)System.Net.EPOS.EKT.SecurityProtocolType.Tls11) | ((SecurityProtocolType)System.Net.EPOS.EKT.SecurityProtocolType.Tls12);
                ServicePointManager.ServerCertificateValidationCallback = ((sender, certificate, chain, sslPolicyErrors) => true);
                string json = string.Empty, body = string.Empty;
                var httpWebRequest = (HttpWebRequest)WebRequest.Create(URL);
                httpWebRequest.ContentType = "application/json";
                httpWebRequest.Method = "POST";
                httpWebRequest.Headers.Add("user", User);
                httpWebRequest.Headers.Add("password", pwd);
                httpWebRequest.Headers.Add("ip", myIP);

                using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
                {
                    streamWriter.Write(JsonPagosLigeros);
                    streamWriter.Flush();
                    streamWriter.Close();
                }

                var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
                StreamReader reader = new StreamReader(httpResponse.GetResponseStream());
                body = reader.ReadToEnd();

                if (body != null && body.Length > 0)
                {
                    RespuestaJSON = body.ToString().Trim();
                    System.Diagnostics.Trace.WriteLine("SI se recupero respuesta del servico pagos ligeros BAZ. Joson:  -->    " + RespuestaJSON, "log");
                }
                else
                {
                    RespuestaJSON = null;
                    System.Diagnostics.Trace.WriteLine("NO se pudo recuperar respuesta del servico pagos ligeros BAZ.", "log");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine("Se detecto un erro en ConsultaPagosLigeros. *Mensaje: " + ex.Message + "    *Detalle error: " + ex.InnerException.Message + "   *StackTrace: " + ex.StackTrace, "log");
                RespuestaJSON = null;
            }
            System.Diagnostics.Trace.WriteLine("Termina ConsultaPagosLigeros.", "log");
            return RespuestaJSON;
        }

        public List<ProductoRenovacionItalika> ObtenerDatosProductosRenovacionITK(string cadenaSkus, int plazo, int pais, int canal, int tienda)
        {
            List<ProductoRenovacionItalika> productosRenovacionItalika = new List<ProductoRenovacionItalika>();
            string trace = "ObtenerDatosProductosRenovacionITK - ";
            try
            {
                Trace.WriteLine(trace + "cadenaSkus: " + cadenaSkus + " Plazo: " + plazo.ToString(), "log");
                EntConsultasBDNST consultaBD = new EntConsultasBDNST();
                string[] skus = cadenaSkus.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);


                foreach (string sku in skus)
                {
                    Trace.WriteLine(trace + "foreach: " + sku.ToString(), "log");
                    DataSet infoSKU = consultaBD.ConsultaProductoPorSKU(Convert.ToInt32(sku));
                    if (infoSKU != null && infoSKU.Tables != null && infoSKU.Tables.Count > 0 && infoSKU.Tables[0].Rows.Count > 0)
                    {
                        //bool esValido = true;
                        int fiProdStat = int.Parse(infoSKU.Tables[0].Rows[0]["fiProdStat"].ToString());
                        int fiExistencia = int.Parse(infoSKU.Tables[0].Rows[0]["fiExistencia"].ToString());
                        if (fiProdStat == 0 && fiExistencia == 0)
                        {
                            //esValido = false;
                            Trace.WriteLine(trace + "SKU: " + sku.ToString() + "fiProdStat: " + fiProdStat.ToString() + "fiExistencia: " + fiExistencia.ToString(), "log");
                        }
                        else
                        {
                            ProductoRenovacionItalika producto = new ProductoRenovacionItalika();
                            float montoAbono = 0;
                            decimal descuento = 0;
                            decimal precioLista = 0;
                            producto.modelo = infoSKU.Tables[0].Rows[0]["fcModelo"].ToString();
                            producto.descripcion = infoSKU.Tables[0].Rows[0]["fcProdDesc"].ToString();
                            producto.sku = int.Parse(infoSKU.Tables[0].Rows[0]["fiProdId"].ToString());
                            producto.tipoAbono = "Semanales";
                            DataSet consultaAbono = consultaBD.ConsultaAbonoPaquetes(sku, plazo);
                            if (consultaAbono != null && consultaAbono.Tables != null && consultaAbono.Tables.Count > 0 && consultaAbono.Tables[0].Rows.Count > 0)
                            {
                                montoAbono = float.Parse(consultaAbono.Tables[0].Rows[0]["fnAbonoPP"].ToString());
                                descuento = decimal.Parse(consultaAbono.Tables[0].Rows[0]["fnDescuento"].ToString());
                                precioLista = decimal.Parse(consultaAbono.Tables[0].Rows[0]["fnMontoVenta"].ToString());
                            }
                            else
                            {
                                Trace.WriteLine(trace + "No se Obtuvieron datos para el plazo consultado (ConsultaAbonoPaquetes)", "log");
                            }

                            Trace.WriteLine(trace + "." + this.ConsultaParametroNegocio(181) + ".", "log");

                            if (this.ConsultaParametroNegocio(181) == "1")
                            {
                                Trace.WriteLine(trace + " Busca abonos en las API", "log");
                                ManejadorAPISCredito apicredito = new ManejadorAPISCredito();
                                EntCarritoRequestVenta carritoApi = new EntCarritoRequestVenta();
                                EntDetalleVentaBaseNST entDetalleVentaBaseNST = new EntDetalleVentaBaseNST();

                                entDetalleVentaBaseNST.montoDescuento = descuento;
                                entDetalleVentaBaseNST.montoEnganche = 0;
                                entDetalleVentaBaseNST.plazos = new[] { plazo };
                                entDetalleVentaBaseNST.SKU = Convert.ToInt32(sku);
                                entDetalleVentaBaseNST.Cantidad = 1;
                                entDetalleVentaBaseNST.precioLista = precioLista;


                                carritoApi.tienda = tienda;
                                carritoApi.canal = canal;
                                carritoApi.pais = pais;
                                carritoApi.tipoCliente = "3";
                                carritoApi.tipoPromocion = "0";
                                carritoApi.tipoProducto = null;
                                carritoApi.tipoEtiquetado = 1;
                                carritoApi.lstEntDetalleVentaBaseNST.Add(entDetalleVentaBaseNST);

                                EntPlazosVenta resp = apicredito.APICotizarVentaCredito(carritoApi);

                                if (resp.oEntRespuestaNST.eTipoError == 0)
                                {
                                    foreach (var item in resp.LstPlazosSKU)
                                    {
                                        if (item.SKU == Convert.ToInt32(sku))
                                        {
                                            foreach (var plazoAbono in item.lstEntPlazoAbono)
                                            {
                                                if (plazoAbono.plazo == plazo)
                                                {
                                                    producto.montoAbono = (int)plazoAbono.abonoPuntual;
                                                    break;
                                                }
                                            }
                                            break;
                                        }
                                    }
                                    Trace.WriteLine(trace + "Información del Producto: \r\n" +
                                                        "SKU: " + producto.sku.ToString() + "\r\n" +
                                                        "Modelo: " + producto.modelo + "\r\n" +
                                                        "Descripcion: " + producto.descripcion + "\r\n" +
                                                        "MontoAbono: " + producto.montoAbono + " " + producto.tipoAbono, "log");

                                }
                                else
                                {
                                    Trace.WriteLine(trace + "Error en el método ManejadorAPISCredito.APICotizarVentaCredito(): " + sku.ToString() + "; Mensaje: " + resp.oEntRespuestaNST.mensajeError, "log");
                                    Trace.WriteLine(trace + "Información del Producto: \r\n" +
                                                        "SKU: " + producto.sku.ToString() + "\r\n" +
                                                        "Modelo: " + producto.modelo + "\r\n" +
                                                        "Descripcion: " + producto.descripcion + "\r\n" +
                                                        "MontoAbono: " + producto.montoAbono + " " + producto.tipoAbono, "log");

                                }
                            }
                            else
                            {
                                Trace.WriteLine(trace + " Busca abonos en la BD local", "log");
                                producto.montoAbono = (int)montoAbono;
                            }
                            //producto.montoAbono = (int)montoAbono;
                            Trace.WriteLine(trace + "Información del Producto: \r\n" +
                                                        "SKU: " + producto.sku.ToString() + "\r\n" +
                                                        "Modelo: " + producto.modelo + "\r\n" +
                                                        "Descripcion: " + producto.descripcion + "\r\n" +
                                                        "MontoAbono: " + producto.montoAbono + " " + producto.tipoAbono, "log");
                            productosRenovacionItalika.Add(producto);
                        }
                    }
                    else
                    {
                        Trace.WriteLine(trace + "No se Obtuvieron datos para el sku consultado (ConsultaProductoPorSKU): " + sku.ToString(), "log");
                    }
                }

                return productosRenovacionItalika;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine("Error al consultar los datos de los productos de Renovacion de Italika, cadena consultada: " + cadenaSkus + ". Msj: " + ex.Message + ". Trace: " + ex.StackTrace, "LOG");
                throw ex;
            }

        }

        public EntRespProductosRenovacionItalika ConsultarProductosRenovacionItalika(int pais, int canal, int tienda, int sku, int plazo, string WS)
        {
            EntRespProductosRenovacionItalika respuRenovacionItalika = new EntRespProductosRenovacionItalika();
            string huella = string.Empty;
            string urlServicioRenovacionesITK = string.Empty;
            try
            {
                huella = "ConsultarProductosRenovacionItalika - ";

                urlServicioRenovacionesITK = new Elektra.Negocio.Entidades.Tienda.EntCatalogoGenerico().ObtenerCatalogo(317).Select(c => c.Descripcion).FirstOrDefault(c => c.Contains("/WSProductoSugeridosITK/"));

                Trace.WriteLine(huella + "Peticion: " +
                                pais.ToString() + " ," +
                                canal.ToString() + " ," +
                                tienda.ToString() + " ," +
                                sku.ToString() + " ," +
                                plazo.ToString() + " ," +
                                WS.ToString() + " ," +
                                urlServicioRenovacionesITK, "log");
                string cadenaSkus = string.Empty;

                WSConsultaRenovacionesItalika wsRenItalika = new WSConsultaRenovacionesItalika(urlServicioRenovacionesITK);

                Elektra.Negocio.NewServicioTienda.WSConsultaRenovacionesItalika.RespProductos productosSugeridos = wsRenItalika.Renovaciones(pais, canal, tienda, sku);


                if (productosSugeridos.Mensaje != null)
                {
                    if (productosSugeridos.Mensaje.EsError)
                    {
                        if (productosSugeridos.Mensaje.CodigoMsj == "201")
                        {
                            Trace.WriteLine(huella + "El servicio de Renovaciones Italika regresó código 201. No se cuenta con productos sugeridos para el sku consultado. SKU: " + sku.ToString(), "log");
                            respuRenovacionItalika.oEntRespuestaNST.eTipoError = EnumTipoErrorNST.Warning;
                            respuRenovacionItalika.oEntRespuestaNST.mensajeError = productosSugeridos.Mensaje.Descripcion;

                            return respuRenovacionItalika;
                        }
                        Trace.WriteLine(huella + "El servicio de Renovaciones Italika regresó Error código:" + productosSugeridos.Mensaje.CodigoMsj
                                + ". Mensaje del servicio: " + productosSugeridos.Mensaje.Descripcion, "log");
                        respuRenovacionItalika.oEntRespuestaNST.eTipoError = EnumTipoErrorNST.Error;
                        respuRenovacionItalika.oEntRespuestaNST.mensajeError = productosSugeridos.Mensaje.Descripcion;

                        return respuRenovacionItalika;
                        //throw new Exception("El servicio de Renovaciones Italika regresó un error - Código de respuesta: " + productosSugeridos.Mensaje.CodigoMsj + ". Mensaje del servidor: " + productosSugeridos.Mensaje.Descripcion);
                    }
                    else
                    {
                        if (productosSugeridos.Productos != null)
                        {
                            foreach (ProductoRenITK producto in productosSugeridos.Productos)
                            {
                                cadenaSkus += producto.SKU.ToString() + ",";
                            }
                            Trace.WriteLine(huella + "Cadena de SKUs recuperados por el Servicio de Renovaciones de Italika: " + cadenaSkus.ToString(), "log");

                            List<ProductoRenovacionItalika> productosRenovacionItalika = ObtenerDatosProductosRenovacionITK(cadenaSkus, plazo, pais, canal, tienda);


                            if (productosSugeridos.Productos.Count > 0 && productosRenovacionItalika.Count == 0)
                            {
                                Trace.WriteLine(huella + "Los productos devueltos por el Servicio de Renovaciones Italika no se encuentran disponibles en Tienda.", "log");
                                respuRenovacionItalika.oEntRespuestaNST.eTipoError = EnumTipoErrorNST.Warning;
                                respuRenovacionItalika.oEntRespuestaNST.mensajeError = "Los productos devueltos por el Servicio de Renovaciones Italika no se encuentran disponibles en Tienda.";

                                return respuRenovacionItalika;
                            }

                            respuRenovacionItalika.ProductosRenovacion = productosRenovacionItalika;
                            return respuRenovacionItalika;
                        }
                    }
                }
                else
                {
                    respuRenovacionItalika.oEntRespuestaNST.eTipoError = EnumTipoErrorNST.Error;
                    respuRenovacionItalika.oEntRespuestaNST.mensajeError = "Error al consultar los productos de Renovacion en el Servicio de Italika. El servicio regresó Mensaje = null.";
                }

                return respuRenovacionItalika;
            }
            catch (Exception ex)
            {
                Trace.WriteLine(huella + "Error en el método ConsultarProductosRenovacionItalika. " +
                                                                        "exMessage: " + ex.Message + " exStackTrace: " + ex.StackTrace, "log");
                respuRenovacionItalika.oEntRespuestaNST.eTipoError = EnumTipoErrorNST.Error;
                respuRenovacionItalika.oEntRespuestaNST.mensajeError = "Error en el método ConsultarProductosRenovacionItalika. " +
                                                                        "exMessage: " + ex.Message + " exStackTrace: " + ex.StackTrace;

                return respuRenovacionItalika;
            }

        }



        #endregion

        #region Métodos privados
        private EntVentaActualCotizar[] CreaLstEntVentaActualCotizar(List<EntDetalleVentaBaseNST> lstEntDetalleVentaBaseNST, EntComplementosNST oEntComplementosNST)
        {
            return this.CreaLstEntVentaActualCotizar(lstEntDetalleVentaBaseNST, EnumTipoVenta.contado, new EntEngancheNST(), 0, oEntComplementosNST);
        }

        private EntVentaActualCotizar[] CreaLstEntVentaActualCotizar(List<EntDetalleVentaBaseNST> lstEntDetalleVentaBaseNST, EnumTipoVenta eTipoVenta, EntEngancheNST oEntEngancheNST, int plazoSeleccionado, EntComplementosNST oEntComplementosNST)
        {
            DetalleVentaBonoNST[] LstProdCanjeTemp = this.lstDetalleVentaBonoAux;
            EntVentaActualCotizar[] lstEntVentaActualCotizar = new EntVentaActualCotizar[1];
            EntVentaActualCotizar oEntVentaActualCotizar = new EntVentaActualCotizar();
            if (oEntComplementosNST != null && oEntComplementosNST.oEntClienteEntregaDomicilioNST != null)
                oEntVentaActualCotizar.clienteEntregaDom = this.CrearClienteEntregaDomicilio(oEntComplementosNST.oEntClienteEntregaDomicilioNST);
            else
                oEntVentaActualCotizar.clienteEntregaDom = new ClienteEntregaDomicilio();

            oEntVentaActualCotizar.lstAtributos = new Atributo[1];
            if (oEntComplementosNST.MontoPromocion > 0)
            {
                oEntVentaActualCotizar.lstAtributos = new Atributo[2];
                Atributo oAtributoDescuento = new Atributo();
                oAtributoDescuento.Key = "DescuentoOUI";
                oAtributoDescuento.Value = oEntComplementosNST.MontoPromocion.ToString();
                oEntVentaActualCotizar.lstAtributos[1] = oAtributoDescuento;
            }

            Atributo oAtributoVenta = new Atributo();
            oAtributoVenta.Key = "idVenta";
            oAtributoVenta.Value = "1";
            oEntVentaActualCotizar.lstAtributos[0] = oAtributoVenta;

            oEntVentaActualCotizar.oEntAvisameIpad = new EntAvisameIpad();
            oEntVentaActualCotizar.oEntPromocionPolicia = new EntPromocionPolicia();
            oEntVentaActualCotizar.oEntTarjetaAzteca = new EntTarjetaAzteca();
            oEntVentaActualCotizar.TipoVenta = eTipoVenta;
            oEntVentaActualCotizar.PlazoSeleccionado = plazoSeleccionado;

            if (oEntEngancheNST.esPorcentajeEng && oEntEngancheNST.porcentajeEnganche > 0)
            {
                oEntVentaActualCotizar.esEngancheModificado = true;
                //decimal monto = 0, porcentaje = 0;
                //monto = Math.Round((oEntEngancheNST.montoFinanciar * (oEntEngancheNST.porcentajeEnganche) / 100), MidpointRounding.ToEven);
                //porcentaje = Math.Round(((monto * 100) / oEntEngancheNST.montoFinanciar), 2);
                //oEntEngancheNST.montoEnganche = monto;
                //oEntEngancheNST.porcentajeEnganche = porcentaje;
                //oEntVentaActualCotizar.MontoEngancheVenta = monto;
                oEntVentaActualCotizar.MontoEngancheVenta = 0;
                oEntVentaActualCotizar.PorcentajeEngancheVenta = oEntEngancheNST.porcentajeEnganche;
            }
            else if (oEntEngancheNST.montoEnganche > 0)
            {
                oEntVentaActualCotizar.esEngancheModificado = true;
                oEntVentaActualCotizar.MontoEngancheVenta = oEntEngancheNST.montoEnganche;
                oEntVentaActualCotizar.PorcentajeEngancheVenta = 0;
                //if (oEntEngancheNST.montoFinanciar > 0)
                //    oEntEngancheNST.porcentajeEnganche = Math.Round(((oEntEngancheNST.montoEnganche * 100) / oEntEngancheNST.montoFinanciar), 2);
            }

            for (int i = 0; i < lstEntDetalleVentaBaseNST.Count; i++)
            {
                if (oEntVentaActualCotizar.ListaDetalleVenta == null)
                    oEntVentaActualCotizar.ListaDetalleVenta = new DetalleVentaBase[lstEntDetalleVentaBaseNST.Count];

                switch (lstEntDetalleVentaBaseNST[i].eTipoPeriodoNST)
                {
                    case EnumPeriodoNST.QuincenalN:
                        oEntVentaActualCotizar.oEntPeriodosIpad = EnumPeriodos.QuincenalN;
                        break;
                    case EnumPeriodoNST.MensualN:
                        oEntVentaActualCotizar.oEntPeriodosIpad = EnumPeriodos.MensualN;
                        break;
                    default:
                        oEntVentaActualCotizar.oEntPeriodosIpad = EnumPeriodos.Semanal;
                        break;
                }

                if (lstEntDetalleVentaBaseNST[i].eTipoAgregadoNST == EnumTipoAgregadoNST.CatalogoExtendido)
                    oEntVentaActualCotizar.EsCatalogoExtendido = true;

                int totMileniasAux = 0;
                DetalleVentaBase oDetalleVentaBase = new DetalleVentaBase();
                oDetalleVentaBase.DescripcionNegocioPlan = "";
                oDetalleVentaBase.DescripcionPlan = "";
                oDetalleVentaBase.lstAddOn = new DatosAddOn[0];
                oDetalleVentaBase.lstAtributos = new Atributo[0];
                string IdsBonoMismaVta = string.Empty;

                oDetalleVentaBase.lstSeries = new EntSeries[lstEntDetalleVentaBaseNST[i].lstSeries.Count];
                oDetalleVentaBase.oPromocionAplicada = new PromocionAplicadaBase[lstEntDetalleVentaBaseNST[i].lstEntPromocionAplicadaNST.Count];
                if (lstEntDetalleVentaBaseNST[i].lstEntDetalleRegaloNST != null)
                    oDetalleVentaBase.lstEntDetalleRegaloDisp = new EntDetalleRegaloDisp[lstEntDetalleVentaBaseNST[i].lstEntDetalleRegaloNST.Count];
                else
                {
                    lstEntDetalleVentaBaseNST[i].lstEntDetalleRegaloNST = new List<EntDetalleRegaloNST>();
                    oDetalleVentaBase.lstEntDetalleRegaloDisp = new EntDetalleRegaloDisp[lstEntDetalleVentaBaseNST[i].lstEntDetalleRegaloNST.Count];
                }

                oDetalleVentaBase.Cantidad = lstEntDetalleVentaBaseNST[i].Cantidad;
                oDetalleVentaBase.SKU = lstEntDetalleVentaBaseNST[i].SKU;
                oDetalleVentaBase.PrecioSugerido = lstEntDetalleVentaBaseNST[i].PrecioSugerido;

                if (lstEntDetalleVentaBaseNST[i].lstOmitirPromociones != null && lstEntDetalleVentaBaseNST[i].lstOmitirPromociones.Count > 0)
                {
                    oDetalleVentaBase.lstOmitirPromociones = new double[lstEntDetalleVentaBaseNST[i].lstOmitirPromociones.Count];
                    for (int p = 0; p < lstEntDetalleVentaBaseNST[i].lstOmitirPromociones.Count; p++)
                    {
                        oDetalleVentaBase.lstOmitirPromociones[p] = lstEntDetalleVentaBaseNST[i].lstOmitirPromociones[p];
                    }
                }

                if (lstEntDetalleVentaBaseNST[i].oEntPlanNST != null)
                {
                    oDetalleVentaBase.NegocioPlan = lstEntDetalleVentaBaseNST[i].oEntPlanNST.NegocioPlan;
                    oDetalleVentaBase.IdPlan = lstEntDetalleVentaBaseNST[i].oEntPlanNST.IdPlan;
                    oDetalleVentaBase.DescripcionPlan = lstEntDetalleVentaBaseNST[i].oEntPlanNST.DescripcionPlan;
                    oDetalleVentaBase.DescripcionNegocioPlan = lstEntDetalleVentaBaseNST[i].oEntPlanNST.DescripcionNegocio;
                }

                totMileniasAux = lstEntDetalleVentaBaseNST[i].Cantidad < lstEntDetalleVentaBaseNST[i].lstEntServicioSeleccionadoNST.Count ? lstEntDetalleVentaBaseNST[i].Cantidad : lstEntDetalleVentaBaseNST[i].lstEntServicioSeleccionadoNST.Count;
                oDetalleVentaBase.lstMileniasSeleccionadas = new EntMileniaSeleccionada[totMileniasAux];
                oDetalleVentaBase.eTipoAgregadoMilenia = (EnumTipoAgregadoMilenia)Convert.ToInt32(lstEntDetalleVentaBaseNST[i].eTipoAgregadoMileniaNST);
                for (int m = 0; m < totMileniasAux; m++)
                {
                    switch (lstEntDetalleVentaBaseNST[i].lstEntServicioSeleccionadoNST[m].eTipoServicio)
                    {
                        case EnumTipoServicioNST.milenia:
                            EntMileniaSeleccionada oEntMileniaSeleccionada = new EntMileniaSeleccionada();
                            oEntMileniaSeleccionada.SKU = lstEntDetalleVentaBaseNST[i].lstEntServicioSeleccionadoNST[m].Sku;
                            oEntMileniaSeleccionada.SKUSpecified = oEntMileniaSeleccionada.SobreprecioSpecified = true;
                            oDetalleVentaBase.lstMileniasSeleccionadas[m] = oEntMileniaSeleccionada;
                            break;
                        case EnumTipoServicioNST.seguroDanios:
                            string comp = string.Empty;
                            oDetalleVentaBase.lstAtributos = new Atributo[2];
                            Atributo oAtributo = new Atributo();
                            oAtributo.Key = "skuPolizaMoto";
                            oAtributo.Value = lstEntDetalleVentaBaseNST[i].lstEntServicioSeleccionadoNST[m].Sku.ToString();
                            oDetalleVentaBase.lstAtributos[0] = oAtributo;
                            Atributo oAtributo2 = new Atributo();
                            oAtributo2.Key = "SeguroMotoSeleccionado";

                            if (lstEntDetalleVentaBaseNST[i].lstEntServicioSeleccionadoNST[m].eTipoCobertura == EnumTipoCobertura.sinSeguro)
                                comp = "dedaños";
                            oAtributo2.Value = comp != string.Empty ? lstEntDetalleVentaBaseNST[i].lstEntServicioSeleccionadoNST[m].eTipoCobertura.ToString().ToLower() + comp :
                              ((int)lstEntDetalleVentaBaseNST[i].lstEntServicioSeleccionadoNST[m].eTipoCobertura).ToString() + ":"
                                + ((int)lstEntDetalleVentaBaseNST[i].lstEntServicioSeleccionadoNST[m].usoSeguro).ToString()
                                + ":" + ((int)lstEntDetalleVentaBaseNST[i].lstEntServicioSeleccionadoNST[m].precioServicio).ToString();
                            oDetalleVentaBase.lstAtributos[1] = oAtributo2;
                            oDetalleVentaBase.lstMileniasSeleccionadas[m] = new EntMileniaSeleccionada();
                            break;
                        default:
                            oDetalleVentaBase.lstMileniasSeleccionadas[m] = new EntMileniaSeleccionada();
                            break;
                    }

                }
                for (int s = 0; s < lstEntDetalleVentaBaseNST[i].lstSeries.Count; s++)
                {
                    EntSeries oEntSeries = new EntSeries();
                    oEntSeries.SERIE = lstEntDetalleVentaBaseNST[i].lstSeries[s].serie;
                    oEntSeries.aplicaDescuentoSerie = lstEntDetalleVentaBaseNST[i].lstSeries[s].aplicaDescuentoSerie;
                    oEntSeries.aplicaDescuentoSerieSpecified = oEntSeries.fiTirSpecified = oEntSeries.NegocioSpecified = oEntSeries.SkuSpecified = true;

                    oDetalleVentaBase.lstSeries[s] = oEntSeries;
                }
                for (int r = 0; r < lstEntDetalleVentaBaseNST[i].lstEntDetalleRegaloNST.Count; r++)
                {
                    EntDetalleRegaloDisp oEntDetalleRegaloDisp = new EntDetalleRegaloDisp();
                    oEntDetalleRegaloDisp.descripcion = lstEntDetalleVentaBaseNST[i].lstEntDetalleRegaloNST[r].descripcion;
                    oEntDetalleRegaloDisp.esSeleccionado = lstEntDetalleVentaBaseNST[i].lstEntDetalleRegaloNST[r].esSeleccionado;
                    oEntDetalleRegaloDisp.idPromocion = lstEntDetalleVentaBaseNST[i].lstEntDetalleRegaloNST[r].promocionId;
                    oEntDetalleRegaloDisp.sku = lstEntDetalleVentaBaseNST[i].lstEntDetalleRegaloNST[r].SKU;
                    oEntDetalleRegaloDisp.esSeleccionadoSpecified = oEntDetalleRegaloDisp.idPromocionSpecified = oEntDetalleRegaloDisp.skuSpecified = true;
                    oDetalleVentaBase.lstEntDetalleRegaloDisp[r] = oEntDetalleRegaloDisp;
                }
                for (int p = 0; p < lstEntDetalleVentaBaseNST[i].lstEntPromocionAplicadaNST.Count; p++)
                {
                    PromocionAplicadaBase oPromocionAplicadaBase = new PromocionAplicadaBase();
                    oPromocionAplicadaBase.Descripcion = lstEntDetalleVentaBaseNST[i].lstEntPromocionAplicadaNST[p].descripcion;
                    oPromocionAplicadaBase.eTipoPromocion = (EnumTipoPromocion)Enum.Parse(typeof(EnumTipoPromocion), lstEntDetalleVentaBaseNST[i].lstEntPromocionAplicadaNST[p].eTipoPromocion.ToString());
                    oPromocionAplicadaBase.MontoOtorgado = lstEntDetalleVentaBaseNST[i].lstEntPromocionAplicadaNST[p].montoOtorgado;
                    oPromocionAplicadaBase.oConvivencia = new Convivencia();
                    oPromocionAplicadaBase.oConvivencia.Milenia = oPromocionAplicadaBase.oConvivencia.MileniaSpecified = true;
                    oPromocionAplicadaBase.oConvivencia.OtrasPromociones = oPromocionAplicadaBase.oConvivencia.OtrasPromocionesSpecified = true;
                    oPromocionAplicadaBase.oConvivencia.OtrosProductos = oPromocionAplicadaBase.oConvivencia.OtrosProductosSpecified = true;
                    oPromocionAplicadaBase.oConvivencia.Seguro = oPromocionAplicadaBase.oConvivencia.SeguroSpecified = true;
                    oPromocionAplicadaBase.oConvivencia.CantidadMaxima = 999999;
                    oPromocionAplicadaBase.oConvivencia.CantidadMaximaSpecified = true;
                    oPromocionAplicadaBase.MontoMaxDescEsp = lstEntDetalleVentaBaseNST[i].lstEntPromocionAplicadaNST[p].montoDispDesc;
                    oPromocionAplicadaBase.NombreCompleto = lstEntDetalleVentaBaseNST[i].lstEntPromocionAplicadaNST[p].NombreCompleto;
                    oPromocionAplicadaBase.DivisionFuerzas = lstEntDetalleVentaBaseNST[i].lstEntPromocionAplicadaNST[p].DivisionFuerzas;
                    oPromocionAplicadaBase.MontoMaxDescEspSpecified = true;
                    oPromocionAplicadaBase.Multiplicidad = 1;
                    oPromocionAplicadaBase.PorcentajeOtorgado = lstEntDetalleVentaBaseNST[i].lstEntPromocionAplicadaNST[p].porcentajeOtorgado;
                    oPromocionAplicadaBase.PromocionId = lstEntDetalleVentaBaseNST[i].lstEntPromocionAplicadaNST[p].promocionId;
                    oPromocionAplicadaBase.SkuRegalo = lstEntDetalleVentaBaseNST[i].lstEntPromocionAplicadaNST[p].skuRegalo;
                    oPromocionAplicadaBase.AplicarDescuentoMonedero = lstEntDetalleVentaBaseNST[i].lstEntPromocionAplicadaNST[p].aplicarDescuentoMonedero;
                    oPromocionAplicadaBase.folioEspecial = lstEntDetalleVentaBaseNST[i].lstEntPromocionAplicadaNST[p].folio;
                    oPromocionAplicadaBase.programa = lstEntDetalleVentaBaseNST[i].lstEntPromocionAplicadaNST[p].programaInstitucional;
                    oPromocionAplicadaBase.eTipoBono = (EnumTipoBono)Convert.ToInt32(lstEntDetalleVentaBaseNST[i].lstEntPromocionAplicadaNST[p].eTipoBonoNST);
                    oPromocionAplicadaBase.SKU_Linea_Otorga = lstEntDetalleVentaBaseNST[i].lstEntPromocionAplicadaNST[p].skuOtorga;

                    oPromocionAplicadaBase.AplicarDescuentoMonederoSpecified = oPromocionAplicadaBase.cantidadSpecified =
                    oPromocionAplicadaBase.eTipoPromocionSpecified = oPromocionAplicadaBase.MontoEngancheSpecified =
                    oPromocionAplicadaBase.MontoOtorgadoSpecified = oPromocionAplicadaBase.MultiplicidadSpecified =
                    oPromocionAplicadaBase.PorcentajeOtorgadoSpecified = oPromocionAplicadaBase.PromocionIdSpecified =
                    oPromocionAplicadaBase.SkuRegaloSpecified = oPromocionAplicadaBase.programaSpecified =
                    oPromocionAplicadaBase.eTipoBonoSpecified = oPromocionAplicadaBase.SKU_Linea_OtorgaSpecified = true;
                    oDetalleVentaBase.oPromocionAplicada[p] = oPromocionAplicadaBase;

                    if (oPromocionAplicadaBase.eTipoPromocion == EnumTipoPromocion.Elektrapesos)
                        IdsBonoMismaVta = IdsBonoMismaVta + oPromocionAplicadaBase.PromocionId.ToString() + ",";
                }
                if (IdsBonoMismaVta.Length > 0)
                    oDetalleVentaBase.lstDetallesBono = this.LlenarProductosBonoPorID(ref LstProdCanjeTemp, IdsBonoMismaVta.Substring(0, IdsBonoMismaVta.Length - 1));
                oDetalleVentaBase.CantidadSpecified = oDetalleVentaBase.IdPlanSpecified = oDetalleVentaBase.MontoEngancheSpecified =
                oDetalleVentaBase.montoVariableTarjetaSpecified = oDetalleVentaBase.NegocioPlanSpecified = oDetalleVentaBase.PrecioMostrarSpecified =
                oDetalleVentaBase.SKUMileniaSpecified = oDetalleVentaBase.SKUSpecified = oDetalleVentaBase.TipoPrecioSpecified = oDetalleVentaBase.eTipoAgregadoMileniaSpecified = oDetalleVentaBase.PrecioSugeridoSpecified = true;
                if (lstEntDetalleVentaBaseNST[i].PrecioRenovacion > 0)
                {
                    var Atributos = (oDetalleVentaBase.lstAtributos == null) ? new List<Atributo>() : oDetalleVentaBase.lstAtributos.ToList();
                    Atributos.Add(new Atributo
                    {
                        Key = "PrecioListaRenovacionOUI",
                        Value = lstEntDetalleVentaBaseNST[i].PrecioRenovacion.ToString()
                    });
                    oDetalleVentaBase.lstAtributos = Atributos.ToArray();
                }
                oEntVentaActualCotizar.ListaDetalleVenta[i] = oDetalleVentaBase;

                if (lstEntDetalleVentaBaseNST[i].accionItk != "undefined" && lstEntDetalleVentaBaseNST[i].accionItk != null && lstEntDetalleVentaBaseNST[i].accionItk != "")
                    oEntVentaActualCotizar.ListaDetalleVenta[i].accionItk = lstEntDetalleVentaBaseNST[i].accionItk;
                else
                    oEntVentaActualCotizar.ListaDetalleVenta[i].accionItk = "NA";

                oEntVentaActualCotizar.esEngancheModificadoSpecified = oEntVentaActualCotizar.esVentaEngancheCeroSpecified = oEntVentaActualCotizar.PorcentajeEngancheVentaSpecified = oEntVentaActualCotizar.MontoEngancheVentaSpecified = oEntVentaActualCotizar.PlazoSeleccionadoSpecified = oEntVentaActualCotizar.skuSeguroSpecified = oEntVentaActualCotizar.TipoVentaSpecified = oEntVentaActualCotizar.oEntPeriodosIpadSpecified = oEntVentaActualCotizar.EsCatalogoExtendidoSpecified = true;

            }



            lstEntVentaActualCotizar[0] = oEntVentaActualCotizar;
            return lstEntVentaActualCotizar;
        }

        private EntRespuestaCotizarVentasNST CreaRespuestaCotizarVentas(RespuestaCotizarVentas oRespuestaCotizarVentas, EntRespuestaNST oEntRespuestaNST, EntComplementosNST oEntComplementosNST)
        {
            EntRespuestaCotizarVentasNST oEntRespuestaCotizarVentasNST = new EntRespuestaCotizarVentasNST();
            bool aplicaFG = false;
            decimal totServicios = 0;

            if (oRespuestaCotizarVentas.ListaVentaActual != null && oRespuestaCotizarVentas.ListaVentaActual.Length > 0)
            {
                if (oRespuestaCotizarVentas.ListaVentaActual[0].ListaDetalleVenta.Length > 0)
                    oEntRespuestaCotizarVentasNST.totalDeVentas = oRespuestaCotizarVentas.TotalDeVentas;

                oEntRespuestaCotizarVentasNST.totalVentaDescuentos = oRespuestaCotizarVentas.TotalVentaDescuentos;

                if (oEntRespuestaNST.eTipoError == EnumTipoErrorNST.SinError)
                    oEntRespuestaCotizarVentasNST.oEntRespuestaNST.eTipoError = (EnumTipoErrorNST)Convert.ToInt32(oRespuestaCotizarVentas.ListaVentaActual[0].Respuesta);
                else
                    oEntRespuestaCotizarVentasNST.oEntRespuestaNST.eTipoError = oEntRespuestaNST.eTipoError;

                oEntRespuestaCotizarVentasNST.oEntRespuestaNST.mensajeError = oEntRespuestaNST.mensajeError + oRespuestaCotizarVentas.ListaVentaActual[0].Mensaje;

                if (oEntComplementosNST != null && oEntComplementosNST.oEntClienteEntregaDomicilioNST != null)
                {
                    oEntRespuestaCotizarVentasNST.oEntComplementosNST.oEntClienteEntregaDomicilioNST = oEntComplementosNST.oEntClienteEntregaDomicilioNST;
                    oEntRespuestaCotizarVentasNST.oEntComplementosNST.oEntClienteEntregaDomicilioNST.entregaCalculada = oRespuestaCotizarVentas.ListaVentaActual[0].oEntEntregaDomIpad.entregaCalculada;
                    oEntRespuestaCotizarVentasNST.oEntComplementosNST.oEntClienteEntregaDomicilioNST.mecanica = oRespuestaCotizarVentas.ListaVentaActual[0].oEntEntregaDomIpad.mecanica;
                    oEntRespuestaCotizarVentasNST.oEntComplementosNST.oEntClienteEntregaDomicilioNST.precioEntrega = oRespuestaCotizarVentas.ListaVentaActual[0].oEntEntregaDomIpad.precioEntrega;
                    oEntRespuestaCotizarVentasNST.oEntComplementosNST.oEntClienteEntregaDomicilioNST.sku = oRespuestaCotizarVentas.ListaVentaActual[0].oEntEntregaDomIpad.sku;
                    oEntRespuestaCotizarVentasNST.oEntComplementosNST.oEntClienteEntregaDomicilioNST.sobreprecioEntrega = oRespuestaCotizarVentas.ListaVentaActual[0].oEntEntregaDomIpad.sobreprecioEntrega;
                }
                if (oEntComplementosNST != null && oEntComplementosNST.oEntVentaRefacciones != null && oEntComplementosNST.oEntVentaRefacciones.DetalleRefacciones != null && oEntComplementosNST.oEntVentaRefacciones.DetalleRefacciones.Folio.Trim().Length > 0)
                    oEntRespuestaCotizarVentasNST.oEntComplementosNST.oEntVentaRefacciones = oEntComplementosNST.oEntVentaRefacciones;

                for (int i = 0; i < oRespuestaCotizarVentas.ListaVentaActual[0].ListaDetalleVenta.Length; i++)
                {
                    bool agregaSeguroDanios = false;
                    EnumTipoCobertura tipoAux = EnumTipoCobertura.sinSeguro;
                    EnumUsoSeguro usoAux = EnumUsoSeguro.Particular;
                    int skuAux = 0;
                    decimal precioAux = 0;

                    EntDetalleVentaResNST oEntDetalleVentaResNST = new EntDetalleVentaResNST();
                    oEntDetalleVentaResNST.Cantidad = oRespuestaCotizarVentas.ListaVentaActual[0].ListaDetalleVenta[i].Cantidad;
                    oEntDetalleVentaResNST.descripcion = oRespuestaCotizarVentas.ListaVentaActual[0].ListaDetalleVenta[i].Descripcion.Trim();
                    oEntDetalleVentaResNST.montoDescuento = oRespuestaCotizarVentas.ListaVentaActual[0].ListaDetalleVenta[i].Descuento;
                    oEntDetalleVentaResNST.precioLista = oRespuestaCotizarVentas.ListaVentaActual[0].ListaDetalleVenta[i].PrecioLista;
                    oEntDetalleVentaResNST.SKU = oRespuestaCotizarVentas.ListaVentaActual[0].ListaDetalleVenta[i].SKU;
                    oEntDetalleVentaResNST.aplicaMSI = this.ValidaMSI(oEntDetalleVentaResNST.SKU.ToString());
                    oEntDetalleVentaResNST.esLiquidacion = oRespuestaCotizarVentas.ListaVentaActual[0].ListaDetalleVenta[i].EsLiquidacion;
                    oEntDetalleVentaResNST.existencia = oRespuestaCotizarVentas.ListaVentaActual[0].ListaDetalleVenta[i].Existencia;
                    oEntDetalleVentaResNST.oEntPlanNST.IdPlan = oRespuestaCotizarVentas.ListaVentaActual[0].ListaDetalleVenta[i].IdPlan;
                    oEntDetalleVentaResNST.oEntPlanNST.NegocioPlan = oRespuestaCotizarVentas.ListaVentaActual[0].ListaDetalleVenta[i].NegocioPlan;
                    oEntDetalleVentaResNST.oEntPlanNST.DescripcionPlan = oRespuestaCotizarVentas.ListaVentaActual[0].ListaDetalleVenta[i].DescripcionPlan;
                    oEntDetalleVentaResNST.oEntPlanNST.DescripcionNegocio = oRespuestaCotizarVentas.ListaVentaActual[0].ListaDetalleVenta[i].DescripcionNegocioPlan;
                    oEntDetalleVentaResNST.eTipoAgregadoMileniaNST = (EnumTipoAgregadoMileniaNST)Convert.ToInt32(oRespuestaCotizarVentas.ListaVentaActual[0].ListaDetalleVenta[i].eTipoAgregadoMilenia);
                    oEntDetalleVentaResNST.pagoPuntualDetalle = oRespuestaCotizarVentas.ListaVentaActual[0].ListaDetalleVenta[i].pagoPuntualDetalle;
                    oEntDetalleVentaResNST.abonoProducto = oRespuestaCotizarVentas.ListaVentaActual[0].ListaDetalleVenta[i].AbonoProducto;
                    oEntDetalleVentaResNST.abonoPPProducto = oRespuestaCotizarVentas.ListaVentaActual[0].ListaDetalleVenta[i].AbonoPPProducto;
                    oEntDetalleVentaResNST.ultabonoProducto = oRespuestaCotizarVentas.ListaVentaActual[0].ListaDetalleVenta[i].UltAbonoProducto;
                    oEntDetalleVentaResNST.PrecioSugerido = oRespuestaCotizarVentas.ListaVentaActual[0].ListaDetalleVenta[i].PrecioSugerido;
                    oEntDetalleVentaResNST.tipoBloqueo = oRespuestaCotizarVentas.ListaVentaActual[0].ListaDetalleVenta[i].TipoBloqueo;
                    oEntDetalleVentaResNST.DeptoProd = oRespuestaCotizarVentas.ListaVentaActual[0].ListaDetalleVenta[i].DeptoProd;
                    oEntDetalleVentaResNST.tienePaquetes = ValidaSiTienePaquete(oRespuestaCotizarVentas.ListaVentaActual[0].ListaDetalleVenta[i].SKU);

                    if (oRespuestaCotizarVentas.ListaVentaActual[0].ListaDetalleVenta[i].lstOmitirPromociones != null && oRespuestaCotizarVentas.ListaVentaActual[0].ListaDetalleVenta[i].lstOmitirPromociones.Length > 0)
                    {
                        for (int p = 0; p < oRespuestaCotizarVentas.ListaVentaActual[0].ListaDetalleVenta[i].lstOmitirPromociones.Length; p++)
                        {
                            oEntDetalleVentaResNST.lstOmitirPromociones.Add(oRespuestaCotizarVentas.ListaVentaActual[0].ListaDetalleVenta[i].lstOmitirPromociones[p]);
                        }
                    }

                    switch (oRespuestaCotizarVentas.ListaVentaActual[0].ListaDetalleVenta[i].TipoProducto)
                    {
                        case EnumTipoProductos.Comercio:
                            oEntDetalleVentaResNST.eTipoProductoNST = EnumTipoProductoNST.mercancias;
                            break;
                        case EnumTipoProductos.MotosOtraMarca:
                            oEntDetalleVentaResNST.eTipoProductoNST = EnumTipoProductoNST.MotosOtraMarca;
                            break;
                        case EnumTipoProductos.MotosServicioPrepagadoItk:
                            oEntDetalleVentaResNST.eTipoProductoNST = EnumTipoProductoNST.MotosServicioPrepagadoItk;
                            break;
                        case EnumTipoProductos.MotosConSerie:
                        case EnumTipoProductos.Motos:
                            oEntDetalleVentaResNST.eTipoProductoNST = EnumTipoProductoNST.motos;
                            break;
                        case EnumTipoProductos.Telefonia:
                        case EnumTipoProductos.EquipoTelefonia:
                            oEntDetalleVentaResNST.eTipoProductoNST = EnumTipoProductoNST.telefonia;
                            break;
                    }

                    if (!aplicaFG)
                        aplicaFG = new ManejadorPromocionesNST().ValidarFleteGratis(oRespuestaCotizarVentas.ListaVentaActual[0].ListaDetalleVenta[i].SKU);

                    for (int m = 0, mx = oRespuestaCotizarVentas.ListaVentaActual[0].ListaDetalleVenta[i].lstMilenias.Length; m < mx; m++)
                    {

                        EnumTipoServicioNST eTipoServicio = EnumTipoServicioNST.sinServicio;
                        EnumTipoCobertura eTipoCobertura = EnumTipoCobertura.sinSeguro;
                        EnumUsoSeguro eUsoSeguro = EnumUsoSeguro.Particular;
                        for (int a = 0, max = oRespuestaCotizarVentas.ListaVentaActual[0].ListaDetalleVenta[i].lstMilenias[m].lstAtributos.Length; a < max; a++)
                        {
                            if (oRespuestaCotizarVentas.ListaVentaActual[0].ListaDetalleVenta[i].lstMilenias[m].lstAtributos[a].Value.ToUpper() == "MILENIA")
                                eTipoServicio = EnumTipoServicioNST.milenia;
                            else
                                eTipoServicio = EnumTipoServicioNST.seguroDanios;
                        }

                        Atributo[] infoAtt_ = oRespuestaCotizarVentas.ListaVentaActual[0].ListaDetalleVenta[i].lstMilenias[m].lstAtributos;
                        if (infoAtt_ != null && infoAtt_.Count() > 0)
                        {
                            foreach (var obj_ in infoAtt_)
                            {
                                if (obj_.Key == "SeguroMotoSeleccionado")
                                {
                                    string[] cob_ = obj_.Value.Split(Convert.ToChar(":"));
                                    if (cob_ != null && cob_.Length > 1)
                                    {
                                        eTipoCobertura = (EnumTipoCobertura)Convert.ToInt32(cob_[0]);
                                        eUsoSeguro = (EnumUsoSeguro)Convert.ToInt32(cob_[1]);
                                    }
                                }
                            }
                        }
                        EntServicioNST oEntServicioNST = new EntServicioNST(oRespuestaCotizarVentas.ListaVentaActual[0].ListaDetalleVenta[i].lstMilenias[m].SKU,
                                                                            oRespuestaCotizarVentas.ListaVentaActual[0].ListaDetalleVenta[i].lstMilenias[m].Precio,
                                                                            oRespuestaCotizarVentas.ListaVentaActual[0].ListaDetalleVenta[i].lstMilenias[m].Descripcion,
                                                                            eTipoServicio,
                                                                            eTipoCobertura,
                                                                            eUsoSeguro);
                        oEntDetalleVentaResNST.lstMileniasDisponibles.Add(oEntServicioNST);
                    }

                    for (int ms = 0; ms < oRespuestaCotizarVentas.ListaVentaActual[0].ListaDetalleVenta[i].lstMileniasSeleccionadas.Length; ms++)
                    {
                        decimal precio = 0;
                        for (int mds = 0; mds < oRespuestaCotizarVentas.ListaVentaActual[0].ListaDetalleVenta[i].lstMilenias.Length; mds++)
                        {
                            if (oRespuestaCotizarVentas.ListaVentaActual[0].ListaDetalleVenta[i].lstMileniasSeleccionadas[ms].SKU ==
                                oRespuestaCotizarVentas.ListaVentaActual[0].ListaDetalleVenta[i].lstMilenias[mds].SKU)
                            {
                                precio = oRespuestaCotizarVentas.ListaVentaActual[0].ListaDetalleVenta[i].lstMilenias[mds].Precio;
                                totServicios += precio;
                                break;
                            }
                        }
                        EntServicioSeleccionadoNST oEntServicioSeleccionadoNST = new EntServicioSeleccionadoNST(EnumTipoServicioNST.milenia,
                                                                                                                oRespuestaCotizarVentas.ListaVentaActual[0].ListaDetalleVenta[i].lstMileniasSeleccionadas[ms].SKU,
                                                                                                                EnumTipoCobertura.sinSeguro,
                                                                                                                precio,
                                                                                                                oRespuestaCotizarVentas.ListaVentaActual[0].ListaDetalleVenta[i].lstMileniasSeleccionadas[ms].Sobreprecio,
                                                                                                                EnumUsoSeguro.Particular,
                                                                                                                oRespuestaCotizarVentas.ListaVentaActual[0].ListaDetalleVenta[i].lstMileniasSeleccionadas[ms].abonoProducto,
                                                                                                                oRespuestaCotizarVentas.ListaVentaActual[0].ListaDetalleVenta[i].lstMileniasSeleccionadas[ms].abonoPPProducto,
                                                                                                                oRespuestaCotizarVentas.ListaVentaActual[0].ListaDetalleVenta[i].lstMileniasSeleccionadas[ms].ultabonoProducto);
                        oEntDetalleVentaResNST.lstEntServicioSeleccionadoNST.Add(oEntServicioSeleccionadoNST);
                    }

                    for (int sd = 0, mx = oRespuestaCotizarVentas.ListaVentaActual[0].ListaDetalleVenta[i].lstAtributos.Length; sd < mx; sd++)
                    {
                        if (oRespuestaCotizarVentas.ListaVentaActual[0].ListaDetalleVenta[i].lstAtributos[sd].Key == "SeguroMotoSeleccionado")
                        {
                            string[] cobs_ = oRespuestaCotizarVentas.ListaVentaActual[0].ListaDetalleVenta[i].lstAtributos[sd].Value.Split(Convert.ToChar(":"));
                            if (cobs_ != null && cobs_.Length > 1)
                            {
                                tipoAux = (EnumTipoCobertura)Convert.ToInt32(cobs_[0]);
                                usoAux = (EnumUsoSeguro)Convert.ToInt32(cobs_[1]);
                            }
                            agregaSeguroDanios = true;
                        }

                        if (oRespuestaCotizarVentas.ListaVentaActual[0].ListaDetalleVenta[i].lstAtributos[sd].Key == "skuPolizaMoto")
                            skuAux = Convert.ToInt32(oRespuestaCotizarVentas.ListaVentaActual[0].ListaDetalleVenta[i].lstAtributos[sd].Value);

                        if (oRespuestaCotizarVentas.ListaVentaActual[0].ListaDetalleVenta[i].lstAtributos[sd].Key == "precioPolizaMoto")
                            precioAux = Convert.ToDecimal(oRespuestaCotizarVentas.ListaVentaActual[0].ListaDetalleVenta[i].lstAtributos[sd].Value);
                    }

                    if (agregaSeguroDanios)
                    {
                        for (int qm = 0; qm < oEntDetalleVentaResNST.lstMileniasDisponibles.Count; qm++)
                            if (oEntDetalleVentaResNST.lstMileniasDisponibles[qm].Descripcion.ToUpper() == "GARANTÍA DEL PROVEEDOR")
                            {
                                oEntDetalleVentaResNST.lstMileniasDisponibles.Remove(oEntDetalleVentaResNST.lstMileniasDisponibles[qm]);
                                break;
                            }

                        for (int ss = 0; ss < oEntDetalleVentaResNST.lstEntServicioSeleccionadoNST.Count; ss++)
                            if (oEntDetalleVentaResNST.lstEntServicioSeleccionadoNST[ss].eTipoServicio == EnumTipoServicioNST.milenia)
                            {
                                oEntDetalleVentaResNST.lstEntServicioSeleccionadoNST.Remove(oEntDetalleVentaResNST.lstEntServicioSeleccionadoNST[ss]);
                                ss--;
                            }

                        totServicios += precioAux;
                        oEntDetalleVentaResNST.lstEntServicioSeleccionadoNST.Add(new EntServicioSeleccionadoNST(EnumTipoServicioNST.seguroDanios, skuAux, tipoAux, precioAux, 0, usoAux));
                    }

                    if (oEntDetalleVentaResNST.lstEntServicioSeleccionadoNST.Count == 0)
                        oEntDetalleVentaResNST.lstEntServicioSeleccionadoNST.Add(new EntServicioSeleccionadoNST());

                    if (oRespuestaCotizarVentas.ListaVentaActual[0].ListaDetalleVenta[i].lstSeriesValidas != null)
                    {
                        for (int s = 0; s < oRespuestaCotizarVentas.ListaVentaActual[0].ListaDetalleVenta[i].lstSeriesValidas.Length; s++)
                        {
                            EntSerieValidaNST oEntSerieValidaNST = new EntSerieValidaNST();
                            oEntSerieValidaNST.serie = oRespuestaCotizarVentas.ListaVentaActual[0].ListaDetalleVenta[i].lstSeriesValidas[s].serie;
                            oEntSerieValidaNST.porcentajeDesc = Convert.ToDecimal(oRespuestaCotizarVentas.ListaVentaActual[0].ListaDetalleVenta[i].lstSeriesValidas[s].porcentajeDesc);

                            oEntDetalleVentaResNST.lstEntSerieValidaNST.Add(oEntSerieValidaNST);
                        }
                    }
                    if (oRespuestaCotizarVentas.ListaVentaActual[0].ListaDetalleVenta[i].lstSeries != null)
                    {
                        for (int ss = 0; ss < oRespuestaCotizarVentas.ListaVentaActual[0].ListaDetalleVenta[i].lstSeries.Length; ss++)
                        {
                            EntSerieNST oEntSerieNST = new EntSerieNST();
                            oEntSerieNST.aplicaDescuentoSerie = oRespuestaCotizarVentas.ListaVentaActual[0].ListaDetalleVenta[i].lstSeries[ss].aplicaDescuentoSerie;
                            oEntSerieNST.serie = oRespuestaCotizarVentas.ListaVentaActual[0].ListaDetalleVenta[i].lstSeries[ss].SERIE;
                            oEntSerieNST.Negocio = oRespuestaCotizarVentas.ListaVentaActual[0].ListaDetalleVenta[i].lstSeries[ss].Negocio;
                            for (int sd = 0; sd < oRespuestaCotizarVentas.ListaVentaActual[0].ListaDetalleVenta[i].lstAtributos.Length; sd++)
                            {
                                switch (oRespuestaCotizarVentas.ListaVentaActual[0].ListaDetalleVenta[i].lstAtributos[sd].Key)
                                {
                                    case "LongMaxSerie":
                                        oEntSerieNST.LongMaxSerie = Convert.ToInt32(oRespuestaCotizarVentas.ListaVentaActual[0].ListaDetalleVenta[i].lstAtributos[sd].Value);
                                        break;
                                    case "LongMaxIMEI":
                                        oEntSerieNST.LongMaxIMEI = Convert.ToInt32(oRespuestaCotizarVentas.ListaVentaActual[0].ListaDetalleVenta[i].lstAtributos[sd].Value);
                                        break;
                                    case "LongMaxChip":
                                        oEntSerieNST.LongMaxChip = Convert.ToInt32(oRespuestaCotizarVentas.ListaVentaActual[0].ListaDetalleVenta[i].lstAtributos[sd].Value);
                                        break;
                                }
                            }
                            oEntDetalleVentaResNST.lstSeries.Add(oEntSerieNST);
                        }
                    }

                    for (int p = 0; p < oRespuestaCotizarVentas.ListaVentaActual[0].ListaDetalleVenta[i].oPromocionAplicada.Length; p++)
                    {
                        EntPromocionAplicadaNST oEntPromocionAplicadaNST = new EntPromocionAplicadaNST();
                        oEntPromocionAplicadaNST.descripcion = oRespuestaCotizarVentas.ListaVentaActual[0].ListaDetalleVenta[i].oPromocionAplicada[p].Descripcion;
                        oEntPromocionAplicadaNST.montoOtorgado = oRespuestaCotizarVentas.ListaVentaActual[0].ListaDetalleVenta[i].oPromocionAplicada[p].MontoOtorgado;
                        oEntPromocionAplicadaNST.porcentajeOtorgado = oRespuestaCotizarVentas.ListaVentaActual[0].ListaDetalleVenta[i].oPromocionAplicada[p].PorcentajeOtorgado;
                        oEntPromocionAplicadaNST.promocionId = oRespuestaCotizarVentas.ListaVentaActual[0].ListaDetalleVenta[i].oPromocionAplicada[p].PromocionId;
                        oEntPromocionAplicadaNST.skuRegalo = oRespuestaCotizarVentas.ListaVentaActual[0].ListaDetalleVenta[i].oPromocionAplicada[p].SkuRegalo;
                        oEntPromocionAplicadaNST.montoDispDesc = oRespuestaCotizarVentas.ListaVentaActual[0].ListaDetalleVenta[i].oPromocionAplicada[p].MontoMaxDescEsp;
                        oEntPromocionAplicadaNST.NombreCompleto = oRespuestaCotizarVentas.ListaVentaActual[0].ListaDetalleVenta[i].oPromocionAplicada[p].NombreCompleto;
                        oEntPromocionAplicadaNST.DivisionFuerzas = oRespuestaCotizarVentas.ListaVentaActual[0].ListaDetalleVenta[i].oPromocionAplicada[p].DivisionFuerzas;
                        oEntPromocionAplicadaNST.eTipoPromocion = (EnumTipoPromocionNST)Enum.Parse(typeof(EnumTipoPromocionNST), oRespuestaCotizarVentas.ListaVentaActual[0].ListaDetalleVenta[i].oPromocionAplicada[p].eTipoPromocion.ToString());
                        //oEntPromocionAplicadaNST.eTipoPromocion = (EnumTipoPromocionNST)Convert.ToInt32(oRespuestaCotizarVentas.ListaVentaActual[0].ListaDetalleVenta[i].oPromocionAplicada[p].eTipoPromocion);
                        oEntPromocionAplicadaNST.aplicarDescuentoMonedero = oRespuestaCotizarVentas.ListaVentaActual[0].ListaDetalleVenta[i].oPromocionAplicada[p].AplicarDescuentoMonedero;
                        oEntPromocionAplicadaNST.folio = oRespuestaCotizarVentas.ListaVentaActual[0].ListaDetalleVenta[i].oPromocionAplicada[p].folioEspecial;
                        oEntPromocionAplicadaNST.programaInstitucional = oRespuestaCotizarVentas.ListaVentaActual[0].ListaDetalleVenta[i].oPromocionAplicada[p].programa;
                        oEntPromocionAplicadaNST.skuOtorga = oRespuestaCotizarVentas.ListaVentaActual[0].ListaDetalleVenta[i].oPromocionAplicada[p].SKU_Linea_Otorga;
                        oEntPromocionAplicadaNST.eTipoBonoNST = (EnumTipoBonoNST)Convert.ToInt32(oRespuestaCotizarVentas.ListaVentaActual[0].ListaDetalleVenta[i].oPromocionAplicada[p].eTipoBono);
                        oEntDetalleVentaResNST.lstEntPromocionAplicadaNST.Add(oEntPromocionAplicadaNST);
                    }

                    for (int r = 0; r < oRespuestaCotizarVentas.ListaVentaActual[0].ListaDetalleVenta[i].lstEntDetalleRegaloDisp.Length; r++)
                    {
                        EntDetalleRegaloNST oEntDetalleRegaloNST = new EntDetalleRegaloNST();
                        oEntDetalleRegaloNST.descripcion = oRespuestaCotizarVentas.ListaVentaActual[0].ListaDetalleVenta[i].lstEntDetalleRegaloDisp[r].descripcion;
                        oEntDetalleRegaloNST.esSeleccionado = oRespuestaCotizarVentas.ListaVentaActual[0].ListaDetalleVenta[i].lstEntDetalleRegaloDisp[r].esSeleccionado;
                        oEntDetalleRegaloNST.promocionId = oRespuestaCotizarVentas.ListaVentaActual[0].ListaDetalleVenta[i].lstEntDetalleRegaloDisp[r].idPromocion;
                        oEntDetalleRegaloNST.SKU = oRespuestaCotizarVentas.ListaVentaActual[0].ListaDetalleVenta[i].lstEntDetalleRegaloDisp[r].sku;
                        oEntDetalleVentaResNST.lstEntDetalleRegaloNST.Add(oEntDetalleRegaloNST);
                    }
                    oEntRespuestaCotizarVentasNST.lstEntDetalleVentaResNST.Add(oEntDetalleVentaResNST);
                }
            }
            else
            {
                if (oEntRespuestaNST.eTipoError == EnumTipoErrorNST.SinError)
                    oEntRespuestaCotizarVentasNST.oEntRespuestaNST.eTipoError = (EnumTipoErrorNST)Convert.ToInt32(oRespuestaCotizarVentas.Resultado.ExisteError);
                else
                    oEntRespuestaCotizarVentasNST.oEntRespuestaNST.eTipoError = oEntRespuestaNST.eTipoError;

                oEntRespuestaCotizarVentasNST.oEntRespuestaNST.mensajeError = oEntRespuestaNST.mensajeError + oRespuestaCotizarVentas.Resultado.Mensaje;
            }
            if (this.oEntSeguroIpad.SKU > 0)
            {
                EntDetalleVentaResNST oEntDetalleVentaResNST = this.CrearRespuestaDetalleSeguroVida();
                if (oEntDetalleVentaResNST.SKU > 0)
                {
                    oEntRespuestaCotizarVentasNST.lstEntDetalleVentaResNST.Add(oEntDetalleVentaResNST);
                    totServicios += oEntDetalleVentaResNST.precioLista;
                }
            }

            oEntRespuestaCotizarVentasNST.totalVentaServicios = totServicios;
            oEntRespuestaCotizarVentasNST.aplicaFleteGratis = aplicaFG;
            oEntRespuestaCotizarVentasNST.aplicaEntregaDomicilio = oRespuestaCotizarVentas.ListaVentaActual[0].aplicaEntregaDomicilio;
            oEntRespuestaCotizarVentasNST.EsNuevoEsquemaCredito = this.SeConsultaNuevoCredito(1532, 3);

            return oEntRespuestaCotizarVentasNST;
        }

        private List<EntDetalleVentaBaseNST> CreaLstEntDetalleVentaBaseNST(string cadenaSkus)
        {
            List<EntDetalleVentaBaseNST> lstEntDetalleVentaBaseNST = new List<EntDetalleVentaBaseNST>();
            if (!string.IsNullOrEmpty(cadenaSkus))
            {
                string[] skus = cadenaSkus.Split(',');
                for (int i = 0; i < skus.Length; i++)
                {
                    if (!string.IsNullOrEmpty(skus[i]))
                    {
                        EntDetalleVentaBaseNST oEntDetalleVentaBaseNST = new EntDetalleVentaBaseNST();
                        oEntDetalleVentaBaseNST.Cantidad = 1;
                        oEntDetalleVentaBaseNST.SKU = Convert.ToInt32(skus[i]);
                        oEntDetalleVentaBaseNST.eTipoAgregadoMileniaNST = EnumTipoAgregadoMileniaNST.Personalizada;
                        lstEntDetalleVentaBaseNST.Add(oEntDetalleVentaBaseNST);
                    }
                }
            }

            return lstEntDetalleVentaBaseNST;
        }

        public EntRespPedidosRenovacion ConsultarPedidosRenovacion(string paisCU, string canalCU, string sucursalCU, string folioCU, string TirPedidos, string WS)
        {
            EntRespPedidosRenovacion respuRenovacion = new EntRespPedidosRenovacion();
            try
            {
                string FiltroXTir = "false";
                if (TirPedidos.Trim().Length > 0)
                    FiltroXTir = "true";

                string URL = string.Empty;
                string CadenaJson = "{\"PaisCU\":" + paisCU + ",\"CanalCU\":" + canalCU + ",\"SucursalCU\":" + sucursalCU + ",\"FolioCU\":" + folioCU + ",\"FiltroXTir\":" + FiltroXTir + ",\"TirPedidos\":" + TirPedidos + ",\"Ws\":\"" + WS + "\",\"App\":\"EPOS\"}";
                ManejadorConsultaWS consWS = new ManejadorConsultaWS();
                EntCatalogos catalogo = new EntCatalogos();
                DataSet dsParametro = catalogo.ObtenerCatalogoGenericoMaestro(1532, 15);

                if (dsParametro != null && dsParametro.Tables != null && dsParametro.Tables.Count > 0 && dsParametro.Tables[0].Rows.Count > 0)
                {
                    URL = dsParametro.Tables[0].Rows[0]["fcCatDesc"].ToString().Trim();
                }
                else
                {
                    ApplicationException ApEx = new ApplicationException("No fue posible recuperar la URL para el servicio de Renovación.");
                    throw ApEx;
                }

                respuRenovacion.PedidosRenovacion = consWS.ConnectWS(URL, CadenaJson, 60000);
            }
            catch (Exception ex)
            {
                respuRenovacion.oEntRespuestaNST.eTipoError = EnumTipoErrorNST.Error;
                respuRenovacion.oEntRespuestaNST.mensajeError = "Error al consultar los pedidos para renovación: " + ex.Message;
                respuRenovacion.PedidosRenovacion = string.Empty;
            }
            return respuRenovacion;
        }


        public bool RealizaEnvioPresupuestoEmail(int presupuesto, string EmailEnvio)
        {
            try
            {
                System.Diagnostics.Trace.WriteLine("Inicia RealizaEnvioPresupuestoEmail()", "log");
                ManejadorConsultaWS consWS = new ManejadorConsultaWS();
                string URL = this.RecuperaCatalogoGenerico(1681, 15);
                string apiKey = this.RecuperaCatalogoGenerico(1681, 16);
                string CadenaJson = string.Empty;

                if (URL == null || URL.Trim() == string.Empty || apiKey == null || apiKey.Trim() == string.Empty)
                {
                    System.Diagnostics.Trace.WriteLine("No fue posible recuperar la URL o la apiKey del catalogo 1681", "log");
                    return false;
                }

                System.Diagnostics.Trace.WriteLine("URL: " + URL + "  apiKey: " + apiKey, "log");

                EntConsultasBDNST dbo = new EntConsultasBDNST();
                DataSet DsJsonEnvio = dbo.RecuperaJSONPresupuestoEmail(presupuesto, EmailEnvio);

                if (DsJsonEnvio != null && DsJsonEnvio.Tables != null && DsJsonEnvio.Tables.Count > 0 && DsJsonEnvio.Tables[0].Rows.Count > 0)
                    CadenaJson = DsJsonEnvio.Tables[0].Rows[0]["fcJsonEnvio"].ToString().Trim();

                System.Diagnostics.Trace.WriteLine("Presupuesto Email Envio. " + CadenaJson, "LOG");
                string respuesta = consWS.ConnectWS(URL, CadenaJson, 15000);
                System.Diagnostics.Trace.WriteLine("Presupuesto Email Respuesta. " + respuesta, "LOG");

                JavaScriptSerializer js = new JavaScriptSerializer();
                dynamic blogObject = js.Deserialize<dynamic>(respuesta);
                int statusCode = blogObject["code"];

                if (statusCode != 0)
                {
                    string msj = blogObject["message"];
                    System.Diagnostics.Trace.WriteLine("Ocurrió un error al enviar el presupuesto por email." + msj, "log");
                    return false;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine("Presupuesto Email Error. " + ex.Message + " trace: " + ex.StackTrace, "LOG");
                return false;
            }
            System.Diagnostics.Trace.WriteLine("Termina RealizaEnvioPresupuestoEmail()", "log");
            return true;
        }

        private EntVentaRefacciones CrearInfoRefacciones(EntVentaRefaccionesNST InfoRefacciones)
        {
            EntVentaRefacciones EntRefacciones = new EntVentaRefacciones();

            if (InfoRefacciones != null && InfoRefacciones.CteRefacciones != null && InfoRefacciones.DetalleRefacciones != null)
            {
                EntRefacciones.CteRefacciones = new EntClienteRefacciones();
                EntRefacciones.CteRefacciones.fcApellidoMat = InfoRefacciones.CteRefacciones.fcApellidoMat;
                EntRefacciones.CteRefacciones.fcApellidoPat = InfoRefacciones.CteRefacciones.fcApellidoPat;
                EntRefacciones.CteRefacciones.fcCalle = InfoRefacciones.CteRefacciones.fcCalle;
                EntRefacciones.CteRefacciones.fcCiudad = InfoRefacciones.CteRefacciones.fcCiudad;
                EntRefacciones.CteRefacciones.fcColonia = InfoRefacciones.CteRefacciones.fcColonia;
                EntRefacciones.CteRefacciones.fcCP = InfoRefacciones.CteRefacciones.fcCP;
                EntRefacciones.CteRefacciones.fcMedioContacto = InfoRefacciones.CteRefacciones.fcMedioContacto;
                EntRefacciones.CteRefacciones.fcNoExt = InfoRefacciones.CteRefacciones.fcNoExt;
                EntRefacciones.CteRefacciones.fcNoInt = InfoRefacciones.CteRefacciones.fcNoInt;
                EntRefacciones.CteRefacciones.fcNombre = InfoRefacciones.CteRefacciones.fcNombre;
                EntRefacciones.CteRefacciones.fcReferencia = InfoRefacciones.CteRefacciones.fcReferencia;
                EntRefacciones.CteRefacciones.fcRFC = InfoRefacciones.CteRefacciones.fcRFC;
                EntRefacciones.CteRefacciones.fiEdoId = InfoRefacciones.CteRefacciones.fiEdoId;
                EntRefacciones.CteRefacciones.fiPobId = InfoRefacciones.CteRefacciones.fiPobId;

                EntRefacciones.DetalleRefacciones = new EntDetalleRefacciones();
                EntRefacciones.DetalleRefacciones.CodigoGenerico = InfoRefacciones.DetalleRefacciones.CodigoGenerico;
                EntRefacciones.DetalleRefacciones.CodigoGenerico2 = InfoRefacciones.DetalleRefacciones.CodigoGenerico2;
                EntRefacciones.DetalleRefacciones.DescuentoTotal = InfoRefacciones.DetalleRefacciones.DescuentoTotal;
                EntRefacciones.DetalleRefacciones.Folio = InfoRefacciones.DetalleRefacciones.Folio;
                EntRefacciones.DetalleRefacciones.MontoTotal = InfoRefacciones.DetalleRefacciones.MontoTotal;
                EntRefacciones.DetalleRefacciones.MontoTotalIVA = InfoRefacciones.DetalleRefacciones.MontoTotalIVA;

                EntRefacciones.DetalleRefacciones.CodigoGenerico2Specified = EntRefacciones.DetalleRefacciones.CodigoGenericoSpecified = EntRefacciones.DetalleRefacciones.DescuentoTotalSpecified
                    = EntRefacciones.DetalleRefacciones.MontoTotalIVASpecified = EntRefacciones.DetalleRefacciones.MontoTotalSpecified = true;

                EntRefacciones.DetalleRefacciones.DetalleVenta = new EntElementoVtaRef[InfoRefacciones.DetalleRefacciones.DetalleVenta.Count];
                for (int i = 0; i < InfoRefacciones.DetalleRefacciones.DetalleVenta.Count; i++)
                {
                    EntRefacciones.DetalleRefacciones.DetalleVenta[i] = new EntElementoVtaRef();
                    EntRefacciones.DetalleRefacciones.DetalleVenta[i].CantidadConfirmada = InfoRefacciones.DetalleRefacciones.DetalleVenta[i].CantidadConfirmada;
                    EntRefacciones.DetalleRefacciones.DetalleVenta[i].CantidadSolicitada = InfoRefacciones.DetalleRefacciones.DetalleVenta[i].CantidadSolicitada;
                    EntRefacciones.DetalleRefacciones.DetalleVenta[i].Descripcion = InfoRefacciones.DetalleRefacciones.DetalleVenta[i].Descripcion;
                    EntRefacciones.DetalleRefacciones.DetalleVenta[i].Descuento = InfoRefacciones.DetalleRefacciones.DetalleVenta[i].Descuento;
                    EntRefacciones.DetalleRefacciones.DetalleVenta[i].Material = InfoRefacciones.DetalleRefacciones.DetalleVenta[i].Material;
                    EntRefacciones.DetalleRefacciones.DetalleVenta[i].Monto = InfoRefacciones.DetalleRefacciones.DetalleVenta[i].Monto;

                    EntRefacciones.DetalleRefacciones.DetalleVenta[i].CantidadConfirmadaSpecified = EntRefacciones.DetalleRefacciones.DetalleVenta[i].CantidadSolicitadaSpecified
                        = EntRefacciones.DetalleRefacciones.DetalleVenta[i].DescuentoSpecified = EntRefacciones.DetalleRefacciones.DetalleVenta[i].MontoSpecified = true;
                }
            }


            return EntRefacciones;
        }

        private EntMarcadoVentaActual[] CreaLstEntMarcadoVentaActual(List<EntDetalleVentaBaseNST> lstEntDetalleVentaBaseNST, decimal montoTotalVenta, EntDatosVentaApartado oEntDatosVentaApartado, EntComplementosNST oEntComplementosNST)
        {
            return this.CreaLstEntMarcadoVentaActual(null, lstEntDetalleVentaBaseNST, null, montoTotalVenta, EnumTipoVenta.mostrador, oEntDatosVentaApartado, oEntComplementosNST);
        }

        private decimal CalculaSobreprecioMileniaPagosLigeros(List<EntDetalleVentaBaseNST> lstEntDetalleVentaBaseNST, int SKUMIl, decimal sobrepreciomil)
        {
            decimal NuevoSobreprecio = 0;
            int ContadorMilenias = 0;

            for (int d = 0; d < lstEntDetalleVentaBaseNST.Count; d++)
            {
                for (int m = 0; m < lstEntDetalleVentaBaseNST[d].lstEntServicioSeleccionadoNST.Count; m++)
                {
                    if (lstEntDetalleVentaBaseNST[d].lstEntServicioSeleccionadoNST[m].eTipoServicio == EnumTipoServicioNST.milenia)
                    {
                        if (lstEntDetalleVentaBaseNST[d].lstEntServicioSeleccionadoNST[m].Sku == SKUMIl)
                            ContadorMilenias = ContadorMilenias + 1;
                    }
                }
            }

            NuevoSobreprecio = Math.Round((sobrepreciomil / ContadorMilenias), 0);

            return NuevoSobreprecio;
        }

        private EntMarcadoVentaActual[] CreaLstEntMarcadoVentaActual(EntInfoPlazoNST oEntInfoPlazoNST, List<EntDetalleVentaBaseNST> lstEntDetalleVentaBaseNST, EntAccionesCreditoNST oEntAccionesCreditoNST, decimal montoTotalVenta, EnumTipoVenta eTipoVenta, EntDatosVentaApartado oEntDatosVentaApartado, EntComplementosNST oEntComplementosNST)
        {
            var metodo = this.GetType().Name + "." + System.Reflection.MethodBase.GetCurrentMethod().Name;
            _logs.AppendLine(metodo, "Inicia");
            bool EsVentaContado = false;
            DetalleVentaBonoNST[] LstProdCanjeTemp = this.lstDetalleVentaBonoAux;
            EntMarcadoVentaActual[] lstEntMarcadoVentaActual = new EntMarcadoVentaActual[1];
            lstEntMarcadoVentaActual[0] = new EntMarcadoVentaActual();
            lstEntMarcadoVentaActual[0].Ip = "";
            bool EsPagosLigeros = false;
            if (oEntComplementosNST != null)
            {
                lstEntMarcadoVentaActual[0].clienteEntregaDom = this.CrearClienteEntregaDomicilio(oEntComplementosNST.oEntClienteEntregaDomicilioNST);
                lstEntMarcadoVentaActual[0].oEntEntregaDomIpad = this.CrearEntEntregaDomIpad(oEntComplementosNST.oEntClienteEntregaDomicilioNST);
                lstEntMarcadoVentaActual[0].oEntVentaRefacciones = this.CrearInfoRefacciones(oEntComplementosNST.oEntVentaRefacciones);
            }
            else
            {
                lstEntMarcadoVentaActual[0].clienteEntregaDom = this.CrearClienteEntregaDomicilio(null);
                lstEntMarcadoVentaActual[0].oEntEntregaDomIpad = this.CrearEntEntregaDomIpad(null);
                lstEntMarcadoVentaActual[0].oEntVentaRefacciones = this.CrearInfoRefacciones(oEntComplementosNST.oEntVentaRefacciones);
            }


            if (oEntInfoPlazoNST != null && oEntInfoPlazoNST.InfoEscalon != null && oEntInfoPlazoNST.InfoEscalon.Trim().Length > 0)
                EsPagosLigeros = true;

            lstEntMarcadoVentaActual[0].ListaDetalleVenta = new DetalleVentaUniticket[lstEntDetalleVentaBaseNST.Count];
            for (int d = 0; d < lstEntDetalleVentaBaseNST.Count; d++)
            {
                DetalleVentaUniticket oDetalleVentaUniticket = new DetalleVentaUniticket();
                oDetalleVentaUniticket.Cantidad = lstEntDetalleVentaBaseNST[d].Cantidad;
                oDetalleVentaUniticket.DescripcionNegocioPlan = "";
                oDetalleVentaUniticket.DescripcionPlan = "";
                oDetalleVentaUniticket.Descuento = lstEntDetalleVentaBaseNST[d].montoDescuento;
                oDetalleVentaUniticket.DesctoEmpleado = lstEntDetalleVentaBaseNST[d].montoDescuento;
                oDetalleVentaUniticket.DescuentoEspecial = 0;
                oDetalleVentaUniticket.fnDesctoMkt = 0;
                oDetalleVentaUniticket.IdPlan = 0;
                oDetalleVentaUniticket.lstAddOn = new DatosAddOn[0];
                oDetalleVentaUniticket.lstAtributos = new Atributo[0];
                string IdsBonoMismaVta = string.Empty;

                if (lstEntDetalleVentaBaseNST[d].lstOmitirPromociones != null && lstEntDetalleVentaBaseNST[d].lstOmitirPromociones.Count > 0)
                {
                    oDetalleVentaUniticket.lstOmitirPromociones = new double[lstEntDetalleVentaBaseNST[d].lstOmitirPromociones.Count];
                    for (int p = 0; p < lstEntDetalleVentaBaseNST[d].lstOmitirPromociones.Count; p++)
                    {
                        oDetalleVentaUniticket.lstOmitirPromociones[p] = lstEntDetalleVentaBaseNST[d].lstOmitirPromociones[p];
                    }
                }

                if (oEntComplementosNST != null && oEntComplementosNST.oAddressClient != null)
                {
                    if (CtrlReglasGenericas.AplicarRegla(829, lstEntDetalleVentaBaseNST[d].SKU))
                    {
                        string val = _logs.ObjectAJson(oEntComplementosNST.oAddressClient);
                        oDetalleVentaUniticket.lstAtributos = new Atributo[1];
                        oDetalleVentaUniticket.lstAtributos[0] = new Atributo()
                        {
                            Key = "OUIFI",
                            Value = val
                        };
                    }
                }
                oDetalleVentaUniticket.AbonoProducto = lstEntDetalleVentaBaseNST[d].abonoProducto;
                oDetalleVentaUniticket.AbonoPPProducto = lstEntDetalleVentaBaseNST[d].abonoPPProducto;
                oDetalleVentaUniticket.UltAbonoProducto = lstEntDetalleVentaBaseNST[d].ultabonoProducto;
                oDetalleVentaUniticket.PrecioSugerido = lstEntDetalleVentaBaseNST[d].PrecioSugerido;

                oDetalleVentaUniticket.PromocionSinIntereseTAZ = new EntPromocionSinInteresesTAZ();
                if (lstEntDetalleVentaBaseNST[d].PromocionSinInteresesTAZ != null)
                {
                    oDetalleVentaUniticket.PromocionSinIntereseTAZ.AplicaPromocionTAZ = lstEntDetalleVentaBaseNST[d].PromocionSinInteresesTAZ.AplicaPromocionTAZ;
                    oDetalleVentaUniticket.PromocionSinIntereseTAZ.AplicaPromocionTAZSpecified = true;
                    oDetalleVentaUniticket.PromocionSinIntereseTAZ.MontoPromocionTAZ = lstEntDetalleVentaBaseNST[d].PromocionSinInteresesTAZ.MontoPromocionTAZ;
                    oDetalleVentaUniticket.PromocionSinIntereseTAZ.MontoPromocionTAZSpecified = true;
                    oDetalleVentaUniticket.PromocionSinIntereseTAZ.Plazo = lstEntDetalleVentaBaseNST[d].PromocionSinInteresesTAZ.Plazo;
                    oDetalleVentaUniticket.PromocionSinIntereseTAZ.PlazoSpecified = true;
                    oDetalleVentaUniticket.PromocionSinIntereseTAZ.NumeroTarjeta = lstEntDetalleVentaBaseNST[d].PromocionSinInteresesTAZ.NumeroTarjeta;
                }

                oDetalleVentaUniticket.eTipo = (EnumTiposProductoNST)lstEntDetalleVentaBaseNST[d].eTipoProductoNST;

                var listaPlazos = new List<PlazosProducto>();
                if (lstEntDetalleVentaBaseNST[d].descuento > 0)
                {
                    _logs.AppendLine(metodo, "Entra a descuento");
                    _logs.AppendLineJson(metodo, "lstEntDetalleVentaBaseNST", new { lstEntDetalleVentaBaseNST = lstEntDetalleVentaBaseNST[d] });
                    listaPlazos.Add(new PlazosProducto
                    {
                        descuento = Convert.ToDouble(lstEntDetalleVentaBaseNST[d].descuento),
                        descuentoPuntual = Convert.ToDouble(lstEntDetalleVentaBaseNST[d].descuentoPuntual),
                        descuentoPuntualSpecified = true,
                        descuentoSpecified = true,
                        mecanica = lstEntDetalleVentaBaseNST[d].mecanica,
                        mecanicaSpecified = true,
                        nuevaTasa = Convert.ToDouble(lstEntDetalleVentaBaseNST[d].nuevaTasa),
                        nuevaTasaPuntual = Convert.ToDouble(lstEntDetalleVentaBaseNST[d].nuevaTasaPuntual),
                        nuevaTasaPuntualSpecified = true,
                        nuevaTasaSpecified = true,
                        plazo = lstEntDetalleVentaBaseNST[d].plazo,
                        plazoSpecified = true,
                        porcentajeDescuento = Convert.ToDouble(lstEntDetalleVentaBaseNST[d].porcentajeDescuento),
                        porcentajeDescuentoSpecified = true,
                        sku = lstEntDetalleVentaBaseNST[d].sku,
                        skuSpecified = true,
                        sobrePrecioOriginal = Convert.ToDouble(lstEntDetalleVentaBaseNST[d].sobrePrecioOriginal),
                        sobrePrecioOriginalSpecified = true,
                        sobrePrecioPuntual = Convert.ToDouble(lstEntDetalleVentaBaseNST[d].sobrePrecioPuntualOriginal),
                        sobrePrecioPuntualOriginalSpecified = true,
                        tasaOriginal = Convert.ToDouble(lstEntDetalleVentaBaseNST[d].tasaOriginal),
                        tasaOriginalSpecified = true,
                        tasaPuntualOriginal = Convert.ToDouble(lstEntDetalleVentaBaseNST[d].tasaPuntualOriginal),
                        tasaPuntualOriginalSpecified = true
                    });
                    _logs.AppendLineJson(metodo, "Detalle Plazos ", new { listaPlazos = listaPlazos });
                }
                else
                    _logs.AppendLine(metodo, "No entra a descuento");
                oDetalleVentaUniticket.listaPlazos = listaPlazos.ToArray();
                if (lstEntDetalleVentaBaseNST[d].eTipoAgregadoNST == EnumTipoAgregadoNST.CatalogoExtendido)
                    oDetalleVentaUniticket.EsCatalogoExtendido = true;

                oDetalleVentaUniticket.lstMileniasSeleccionadas = new EntMileniaSeleccionada[lstEntDetalleVentaBaseNST[d].lstEntServicioSeleccionadoNST.Count];
                oDetalleVentaUniticket.lstSeries = new EntSeries[lstEntDetalleVentaBaseNST[d].lstSeries.Count];
                oDetalleVentaUniticket.Mecanica = lstEntDetalleVentaBaseNST[d].mecanica;
                oDetalleVentaUniticket.Modalidad = "";
                oDetalleVentaUniticket.MontoEnganche = lstEntDetalleVentaBaseNST[d].montoEnganche;
                if (EsPagosLigeros)
                    oDetalleVentaUniticket.MontoSobrePrecio = Math.Round((lstEntDetalleVentaBaseNST[d].montoSobreprecio / lstEntDetalleVentaBaseNST[d].Cantidad), 0);
                else
                    oDetalleVentaUniticket.MontoSobrePrecio = lstEntDetalleVentaBaseNST[d].montoSobreprecio;
                oDetalleVentaUniticket.montoVariableTarjeta = 0;
                oDetalleVentaUniticket.NegocioPlan = 0;
                oDetalleVentaUniticket.oPromocionAplicada = new PromocionAplicadaBase[lstEntDetalleVentaBaseNST[d].lstEntPromocionAplicadaNST.Count];
                oDetalleVentaUniticket.PrecioLista = oDetalleVentaUniticket.PrecioMostrar = this.ObtenerPrecioProducto(Convert.ToDouble(lstEntDetalleVentaBaseNST[d].SKU));
                if (lstEntDetalleVentaBaseNST[d].PrecioSugerido > 0)
                    oDetalleVentaUniticket.PrecioLista = oDetalleVentaUniticket.PrecioMostrar = lstEntDetalleVentaBaseNST[d].PrecioSugerido;
                oDetalleVentaUniticket.SKU = lstEntDetalleVentaBaseNST[d].SKU;
                oDetalleVentaUniticket.SKUMilenia = 0;
                oDetalleVentaUniticket.SobreprecioMilenia = 0;
                oDetalleVentaUniticket.TipoPrecio = EnumTipoPrecioIpad.ProductoNuevo;

                if (lstEntDetalleVentaBaseNST[d].oEntPlanNST != null)
                {
                    oDetalleVentaUniticket.NegocioPlan = lstEntDetalleVentaBaseNST[d].oEntPlanNST.NegocioPlan;
                    oDetalleVentaUniticket.DescripcionNegocioPlan = lstEntDetalleVentaBaseNST[d].oEntPlanNST.DescripcionNegocio;
                    oDetalleVentaUniticket.IdPlan = lstEntDetalleVentaBaseNST[d].oEntPlanNST.IdPlan;
                    oDetalleVentaUniticket.DescripcionPlan = lstEntDetalleVentaBaseNST[d].oEntPlanNST.DescripcionPlan;
                }

                for (int m = 0; m < lstEntDetalleVentaBaseNST[d].lstEntServicioSeleccionadoNST.Count; m++)
                {
                    switch (lstEntDetalleVentaBaseNST[d].lstEntServicioSeleccionadoNST[m].eTipoServicio)
                    {
                        case EnumTipoServicioNST.milenia:
                            EntMileniaSeleccionada oEntMileniaSeleccionada = new EntMileniaSeleccionada();
                            oEntMileniaSeleccionada.SKU = lstEntDetalleVentaBaseNST[d].lstEntServicioSeleccionadoNST[m].Sku;
                            if (EsPagosLigeros)
                                oEntMileniaSeleccionada.Sobreprecio = CalculaSobreprecioMileniaPagosLigeros(lstEntDetalleVentaBaseNST, lstEntDetalleVentaBaseNST[d].lstEntServicioSeleccionadoNST[m].Sku, lstEntDetalleVentaBaseNST[d].lstEntServicioSeleccionadoNST[m].montoSobreprecio);
                            else
                                oEntMileniaSeleccionada.Sobreprecio = lstEntDetalleVentaBaseNST[d].lstEntServicioSeleccionadoNST[m].montoSobreprecio;
                            oEntMileniaSeleccionada.SKUSpecified = oEntMileniaSeleccionada.SobreprecioSpecified = true;
                            oEntMileniaSeleccionada.abonoProducto = lstEntDetalleVentaBaseNST[d].lstEntServicioSeleccionadoNST[m].abonoProducto;
                            oEntMileniaSeleccionada.abonoPPProducto = lstEntDetalleVentaBaseNST[d].lstEntServicioSeleccionadoNST[m].abonoPPProducto;
                            oEntMileniaSeleccionada.ultabonoProducto = lstEntDetalleVentaBaseNST[d].lstEntServicioSeleccionadoNST[m].ultabonoProducto;
                            oEntMileniaSeleccionada.precioServicio = lstEntDetalleVentaBaseNST[d].lstEntServicioSeleccionadoNST[m].precioServicio;
                            oEntMileniaSeleccionada.abonoProductoSpecified = oEntMileniaSeleccionada.abonoPPProductoSpecified = oEntMileniaSeleccionada.ultabonoProductoSpecified = oEntMileniaSeleccionada.precioServicioSpecified = true;
                            oDetalleVentaUniticket.lstMileniasSeleccionadas[m] = oEntMileniaSeleccionada;
                            break;
                        case EnumTipoServicioNST.seguroDanios:
                            string comp = string.Empty;
                            oDetalleVentaUniticket.lstAtributos = new Atributo[8];
                            Atributo oAtributo = new Atributo();
                            oAtributo.Key = "skuPolizaMoto";
                            oAtributo.Value = lstEntDetalleVentaBaseNST[d].lstEntServicioSeleccionadoNST[m].Sku.ToString();
                            oDetalleVentaUniticket.lstAtributos[0] = oAtributo;
                            Atributo oAtributo2 = new Atributo();
                            oAtributo2.Key = "SeguroMotoSeleccionado";

                            if (lstEntDetalleVentaBaseNST[d].lstEntServicioSeleccionadoNST[m].eTipoCobertura == EnumTipoCobertura.sinSeguro)
                                comp = "dedaños";

                            oAtributo2.Value = lstEntDetalleVentaBaseNST[d].lstEntServicioSeleccionadoNST[m].eTipoCobertura.ToString().ToLower() + comp;
                            oDetalleVentaUniticket.lstAtributos[1] = oAtributo2;
                            Atributo oAtributo3 = new Atributo();
                            oAtributo3.Key = "precioPolizaMoto";
                            oAtributo3.Value = lstEntDetalleVentaBaseNST[d].lstEntServicioSeleccionadoNST[m].precioServicio.ToString();
                            oDetalleVentaUniticket.lstAtributos[2] = oAtributo3;

                            Atributo oAtributo4 = new Atributo();
                            oAtributo4.Key = "sobreprecioPolizaMoto";
                            oAtributo4.Value = lstEntDetalleVentaBaseNST[d].lstEntServicioSeleccionadoNST[m].montoSobreprecio.ToString();
                            oDetalleVentaUniticket.lstAtributos[3] = oAtributo4;

                            //Se guarda el uso del seguro
                            Atributo oAtributo5 = new Atributo();
                            oAtributo5.Key = "usoSeguro";
                            oAtributo5.Value = lstEntDetalleVentaBaseNST[d].lstEntServicioSeleccionadoNST[m].usoSeguro.ToString().ToLower();
                            oDetalleVentaUniticket.lstAtributos[4] = oAtributo5;

                            Atributo oAtributo6 = new Atributo();
                            oAtributo6.Key = "abonoProductoItalika";
                            oAtributo6.Value = lstEntDetalleVentaBaseNST[d].lstEntServicioSeleccionadoNST[m].abonoProducto.ToString();
                            oDetalleVentaUniticket.lstAtributos[5] = oAtributo6;

                            Atributo oAtributo7 = new Atributo();
                            oAtributo7.Key = "abonoPPProductoItalika";
                            oAtributo7.Value = lstEntDetalleVentaBaseNST[d].lstEntServicioSeleccionadoNST[m].abonoPPProducto.ToString();
                            oDetalleVentaUniticket.lstAtributos[6] = oAtributo7;

                            Atributo oAtributo8 = new Atributo();
                            oAtributo8.Key = "ultabonoProductoItalika";
                            oAtributo8.Value = lstEntDetalleVentaBaseNST[d].lstEntServicioSeleccionadoNST[m].ultabonoProducto.ToString();
                            oDetalleVentaUniticket.lstAtributos[7] = oAtributo8;

                            oDetalleVentaUniticket.lstMileniasSeleccionadas[m] = new EntMileniaSeleccionada();
                            break;
                        default:
                            oDetalleVentaUniticket.lstMileniasSeleccionadas[m] = new EntMileniaSeleccionada();
                            break;
                    }
                }
                for (int s = 0; s < lstEntDetalleVentaBaseNST[d].lstSeries.Count; s++)
                {
                    EntSeries oEntSeries = new EntSeries();
                    oEntSeries.SERIE = lstEntDetalleVentaBaseNST[d].lstSeries[s].serie;
                    oEntSeries.aplicaDescuentoSerie = lstEntDetalleVentaBaseNST[d].lstSeries[s].aplicaDescuentoSerie;
                    oEntSeries.aplicaDescuentoSerieSpecified = oEntSeries.fiTirSpecified = oEntSeries.NegocioSpecified = oEntSeries.SkuSpecified = true;

                    oDetalleVentaUniticket.lstSeries[s] = oEntSeries;
                }
                for (int p = 0; p < lstEntDetalleVentaBaseNST[d].lstEntPromocionAplicadaNST.Count; p++)
                {
                    PromocionAplicadaBase oPromocionAplicadaBase = new PromocionAplicadaBase();
                    oPromocionAplicadaBase.cantidad = 1;
                    oPromocionAplicadaBase.Descripcion = lstEntDetalleVentaBaseNST[d].lstEntPromocionAplicadaNST[p].descripcion;
                    oPromocionAplicadaBase.eTipoPromocion = (EnumTipoPromocion)Enum.Parse(typeof(EnumTipoPromocion), lstEntDetalleVentaBaseNST[d].lstEntPromocionAplicadaNST[p].eTipoPromocion.ToString());
                    //oPromocionAplicadaBase.eTipoPromocion = (EnumTipoPromocion)Convert.ToInt32(lstEntDetalleVentaBaseNST[d].lstEntPromocionAplicadaNST[p].eTipoPromocion);
                    oPromocionAplicadaBase.MontoOtorgado = lstEntDetalleVentaBaseNST[d].lstEntPromocionAplicadaNST[p].montoOtorgado;
                    oPromocionAplicadaBase.oConvivencia = new Convivencia();
                    oPromocionAplicadaBase.oConvivencia.Milenia = oPromocionAplicadaBase.oConvivencia.MileniaSpecified = true;
                    oPromocionAplicadaBase.oConvivencia.OtrasPromociones = oPromocionAplicadaBase.oConvivencia.OtrasPromocionesSpecified = true;
                    oPromocionAplicadaBase.oConvivencia.OtrosProductos = oPromocionAplicadaBase.oConvivencia.OtrosProductosSpecified = true;
                    oPromocionAplicadaBase.oConvivencia.Seguro = oPromocionAplicadaBase.oConvivencia.SeguroSpecified = true;
                    oPromocionAplicadaBase.oConvivencia.CantidadMaxima = 999999;
                    oPromocionAplicadaBase.oConvivencia.CantidadMaximaSpecified = true;
                    oPromocionAplicadaBase.Multiplicidad = 1;
                    oPromocionAplicadaBase.MontoMaxDescEsp = lstEntDetalleVentaBaseNST[d].lstEntPromocionAplicadaNST[p].montoDispDesc;
                    oPromocionAplicadaBase.NombreCompleto = lstEntDetalleVentaBaseNST[d].lstEntPromocionAplicadaNST[p].NombreCompleto;
                    oPromocionAplicadaBase.DivisionFuerzas = lstEntDetalleVentaBaseNST[d].lstEntPromocionAplicadaNST[p].DivisionFuerzas;
                    oPromocionAplicadaBase.MontoMaxDescEspSpecified = true;
                    oPromocionAplicadaBase.PorcentajeOtorgado = lstEntDetalleVentaBaseNST[d].lstEntPromocionAplicadaNST[p].porcentajeOtorgado;
                    oPromocionAplicadaBase.PromocionId = lstEntDetalleVentaBaseNST[d].lstEntPromocionAplicadaNST[p].promocionId;
                    oPromocionAplicadaBase.SkuRegalo = lstEntDetalleVentaBaseNST[d].lstEntPromocionAplicadaNST[p].skuRegalo;
                    oPromocionAplicadaBase.AplicarDescuentoMonedero = lstEntDetalleVentaBaseNST[d].lstEntPromocionAplicadaNST[p].aplicarDescuentoMonedero;
                    oPromocionAplicadaBase.folioEspecial = lstEntDetalleVentaBaseNST[d].lstEntPromocionAplicadaNST[p].folio;
                    oPromocionAplicadaBase.programa = lstEntDetalleVentaBaseNST[d].lstEntPromocionAplicadaNST[p].programaInstitucional;
                    oPromocionAplicadaBase.SKU_Linea_Otorga = lstEntDetalleVentaBaseNST[d].lstEntPromocionAplicadaNST[p].skuOtorga;

                    oPromocionAplicadaBase.AplicarDescuentoMonederoSpecified = oPromocionAplicadaBase.cantidadSpecified =
                    oPromocionAplicadaBase.eTipoPromocionSpecified = oPromocionAplicadaBase.MontoEngancheSpecified =
                    oPromocionAplicadaBase.MontoOtorgadoSpecified = oPromocionAplicadaBase.MultiplicidadSpecified =
                    oPromocionAplicadaBase.PorcentajeOtorgadoSpecified = oPromocionAplicadaBase.PromocionIdSpecified =
                    oPromocionAplicadaBase.SkuRegaloSpecified = oPromocionAplicadaBase.programaSpecified = oPromocionAplicadaBase.SKU_Linea_OtorgaSpecified =
                    oPromocionAplicadaBase.eTipoBonoSpecified = true;

                    oDetalleVentaUniticket.oPromocionAplicada[p] = oPromocionAplicadaBase;

                    if (oPromocionAplicadaBase.eTipoPromocion == EnumTipoPromocion.Elektrapesos)
                        IdsBonoMismaVta = IdsBonoMismaVta + oPromocionAplicadaBase.PromocionId.ToString() + ",";
                }
                if (IdsBonoMismaVta.Length > 0)
                    oDetalleVentaUniticket.lstDetallesBono = this.LlenarProductosBonoPorID(ref LstProdCanjeTemp, IdsBonoMismaVta.Substring(0, IdsBonoMismaVta.Length - 1));
                oDetalleVentaUniticket.CantidadSpecified = oDetalleVentaUniticket.DescuentoEspecialSpecified = oDetalleVentaUniticket.DesctoEmpleadoSpecified = oDetalleVentaUniticket.DescuentoSpecified = oDetalleVentaUniticket.fnDesctoMktSpecified = oDetalleVentaUniticket.IdPlanSpecified =
                    oDetalleVentaUniticket.MecanicaSpecified = oDetalleVentaUniticket.MontoEngancheSpecified = oDetalleVentaUniticket.MontoSobrePrecioSpecified = oDetalleVentaUniticket.montoVariableTarjetaSpecified = oDetalleVentaUniticket.NegocioPlanSpecified =
                    oDetalleVentaUniticket.PrecioListaSpecified = oDetalleVentaUniticket.PrecioMostrarSpecified = oDetalleVentaUniticket.SKUMileniaSpecified = oDetalleVentaUniticket.SKUSpecified = oDetalleVentaUniticket.SobreprecioMileniaSpecified = oDetalleVentaUniticket.TipoPrecioSpecified =
                    oDetalleVentaUniticket.AbonoProductoSpecified = oDetalleVentaUniticket.AbonoPPProductoSpecified = oDetalleVentaUniticket.UltAbonoProductoSpecified = oDetalleVentaUniticket.PrecioSugeridoSpecified = oDetalleVentaUniticket.EsCatalogoExtendidoSpecified = true;
                lstEntMarcadoVentaActual[0].ListaDetalleVenta[d] = oDetalleVentaUniticket;
                EsVentaContado = this.AplicaVentaContado(oDetalleVentaUniticket.oPromocionAplicada);
            }
            lstEntMarcadoVentaActual[0].lstAtributos = new Atributo[1];
            Atributo oAtributoVenta = new Atributo();
            oAtributoVenta.Key = "idVenta";
            oAtributoVenta.Value = "1";
            lstEntMarcadoVentaActual[0].lstAtributos[0] = oAtributoVenta;

            lstEntMarcadoVentaActual[0].objCredito = this.CrearCredito(oEntInfoPlazoNST, oEntAccionesCreditoNST);
            lstEntMarcadoVentaActual[0].oEntAvisameIpad = this.CrearEntAvisameIpad();

            lstEntMarcadoVentaActual[0].oEntPromocionPolicia = this.CrearEntPromocionPolicia();
            lstEntMarcadoVentaActual[0].oEntTarjetaAzteca = this.CrearEntTarjetaAzteca();
            lstEntMarcadoVentaActual[0].precioTotal = montoTotalVenta;
            lstEntMarcadoVentaActual[0].TarjetasBancarias = new EntManejadorTarjetaBancaria[0];

            if (this.ContieneClienteContado && EsVentaContado)
                eTipoVenta = EnumTipoVenta.contado;

            lstEntMarcadoVentaActual[0].tipoVenta = eTipoVenta;
            if (oEntDatosVentaApartado != null && oEntDatosVentaApartado.montoApartado > 0)
            {
                lstEntMarcadoVentaActual[0].tipoVenta = EnumTipoVenta.apartado;
                lstEntMarcadoVentaActual[0].MontoTotalVenta = Convert.ToDouble(oEntDatosVentaApartado.montoApartado);
            }
            lstEntMarcadoVentaActual[0].MontoTotalVentaSpecified = lstEntMarcadoVentaActual[0].MontoVentaEfectivoSpecified = lstEntMarcadoVentaActual[0].MontoVentaTarjetaSpecified = lstEntMarcadoVentaActual[0].precioTotalSpecified = lstEntMarcadoVentaActual[0].tipoVentaSpecified = true;
            _logs.EscribeLog();
            return lstEntMarcadoVentaActual;
        }

        private EntMarcadoVentaActualCaja[] CreaLstEntMarcadoVentaActualCaja(List<EntDetalleVentaBaseNST> lstEntDetalleVentaBaseNST, decimal montoTotalVenta, decimal montoTotalEfectivo, List<EntDocumentoProxyNST> lstEntDocumentoProxyNST, EntComplementosNST oEntComplementosNST)
        {
            return this.CreaLstEntMarcadoVentaActualCaja(null, lstEntDetalleVentaBaseNST, null, montoTotalVenta, montoTotalEfectivo, EnumTipoVenta.mostrador, lstEntDocumentoProxyNST, oEntComplementosNST);
        }

        private EntMarcadoVentaActualCaja[] CreaLstEntMarcadoVentaActualCaja(EntInfoPlazoNST oEntInfoPlazoNST, List<EntDetalleVentaBaseNST> lstEntDetalleVentaBaseNST, EntAccionesCreditoNST oEntAccionesCreditoNST, decimal montoTotalVenta, decimal montoTotalEfectivo, EnumTipoVenta eTipoVenta, List<EntDocumentoProxyNST> lstEntDocumentoProxyNST, EntComplementosNST oEntComplementosNST)
        {
            EntMarcadoVentaActualCaja[] lstEntMarcadoVentaActual = new EntMarcadoVentaActualCaja[1];
            lstEntMarcadoVentaActual[0] = new EntMarcadoVentaActualCaja();
            lstEntMarcadoVentaActual[0].Ip = "";
            lstEntMarcadoVentaActual[0].IdSesionCaja = oEntAccionesCreditoNST.idSecionCaja;
            lstEntMarcadoVentaActual[0].documentoProxy = new DocumentoProxy[lstEntDocumentoProxyNST.Count];
            if (oEntComplementosNST != null)
            {
                lstEntMarcadoVentaActual[0].clienteEntregaDom = this.CrearClienteEntregaDomicilio(oEntComplementosNST.oEntClienteEntregaDomicilioNST);
                lstEntMarcadoVentaActual[0].oEntEntregaDomIpad = this.CrearEntEntregaDomIpad(oEntComplementosNST.oEntClienteEntregaDomicilioNST);
            }
            else
            {
                lstEntMarcadoVentaActual[0].clienteEntregaDom = this.CrearClienteEntregaDomicilio(null);
                lstEntMarcadoVentaActual[0].oEntEntregaDomIpad = this.CrearEntEntregaDomIpad(null);
            }
            if (lstEntDocumentoProxyNST.Count > 0)
            {
                for (int i = 0; i < lstEntDocumentoProxyNST.Count; i++)
                {
                    DocumentoProxy oDocumentoProxy = new DocumentoProxy();
                    oDocumentoProxy.Importe = lstEntDocumentoProxyNST[i].importe;
                    oDocumentoProxy.NumeroDocumento = lstEntDocumentoProxyNST[i].numeroDocumento;
                    oDocumentoProxy.TipoDocumento = lstEntDocumentoProxyNST[i].tipoDocumento;
                    switch (lstEntDocumentoProxyNST[i].eIdentificadorTipoPago)
                    {
                        case EnumIdentificadorTipoPagoNST.NoAsignado:
                            oDocumentoProxy.TipoPago = IdentificadorTipoPagoProxy.NoAsignado;
                            break;
                        case EnumIdentificadorTipoPagoNST.TarjetaBancariaProxy:
                            oDocumentoProxy.TipoPago = IdentificadorTipoPagoProxy.TarjetaBancariaProxy;
                            break;
                        case EnumIdentificadorTipoPagoNST.PromoValeProxy:
                            oDocumentoProxy.TipoPago = IdentificadorTipoPagoProxy.PromoValeProxy;
                            break;
                        case EnumIdentificadorTipoPagoNST.BonoProxy:
                            oDocumentoProxy.TipoPago = IdentificadorTipoPagoProxy.BonoProxy;
                            break;
                        case EnumIdentificadorTipoPagoNST.DocumentoCustodiaProxy:
                            oDocumentoProxy.TipoPago = IdentificadorTipoPagoProxy.DocumentoCustodiaProxy;
                            break;
                    }
                    oDocumentoProxy.ImporteSpecified = oDocumentoProxy.TipoDocumentoSpecified = oDocumentoProxy.TipoPagoSpecified = true;
                    lstEntMarcadoVentaActual[0].documentoProxy[i] = oDocumentoProxy;
                }
            }
            lstEntMarcadoVentaActual[0].ListaDetalleVenta = new DetalleVentaUniticket[lstEntDetalleVentaBaseNST.Count];
            for (int d = 0; d < lstEntDetalleVentaBaseNST.Count; d++)
            {
                DetalleVentaUniticket oDetalleVentaUniticket = new DetalleVentaUniticket();
                oDetalleVentaUniticket.Cantidad = lstEntDetalleVentaBaseNST[d].Cantidad;
                oDetalleVentaUniticket.DescripcionNegocioPlan = "";
                oDetalleVentaUniticket.DescripcionPlan = "";
                oDetalleVentaUniticket.Descuento = lstEntDetalleVentaBaseNST[d].montoDescuento;
                oDetalleVentaUniticket.DescuentoEspecial = 0;
                oDetalleVentaUniticket.fnDesctoMkt = 0;
                oDetalleVentaUniticket.IdPlan = 0;
                oDetalleVentaUniticket.lstAddOn = new DatosAddOn[0];
                oDetalleVentaUniticket.lstAtributos = new Atributo[0];
                oDetalleVentaUniticket.lstDetallesBono = new DetalleVentaBono[0];
                oDetalleVentaUniticket.lstMileniasSeleccionadas = new EntMileniaSeleccionada[lstEntDetalleVentaBaseNST[d].lstEntServicioSeleccionadoNST.Count];
                oDetalleVentaUniticket.lstSeries = new EntSeries[lstEntDetalleVentaBaseNST[d].lstSeries.Count];
                oDetalleVentaUniticket.Mecanica = lstEntDetalleVentaBaseNST[d].mecanica;
                oDetalleVentaUniticket.Modalidad = "";
                oDetalleVentaUniticket.MontoEnganche = lstEntDetalleVentaBaseNST[d].montoEnganche;
                oDetalleVentaUniticket.MontoSobrePrecio = lstEntDetalleVentaBaseNST[d].montoSobreprecio;
                oDetalleVentaUniticket.montoVariableTarjeta = 0;
                oDetalleVentaUniticket.NegocioPlan = 0;
                oDetalleVentaUniticket.oPromocionAplicada = new PromocionAplicadaBase[lstEntDetalleVentaBaseNST[d].lstEntPromocionAplicadaNST.Count];
                oDetalleVentaUniticket.PrecioLista = oDetalleVentaUniticket.PrecioMostrar = this.ObtenerPrecioProducto(Convert.ToDouble(lstEntDetalleVentaBaseNST[d].SKU));
                oDetalleVentaUniticket.SKU = lstEntDetalleVentaBaseNST[d].SKU;
                oDetalleVentaUniticket.SKUMilenia = 0;
                oDetalleVentaUniticket.SobreprecioMilenia = 0;
                oDetalleVentaUniticket.TipoPrecio = EnumTipoPrecioIpad.ProductoNuevo;

                if (lstEntDetalleVentaBaseNST[d].eTipoProductoNST == EnumTipoProductoNST.telefonia && !this.esTelefonia)
                    this.esTelefonia = true;

                for (int m = 0; m < lstEntDetalleVentaBaseNST[d].lstEntServicioSeleccionadoNST.Count; m++)
                {
                    switch (lstEntDetalleVentaBaseNST[d].lstEntServicioSeleccionadoNST[m].eTipoServicio)
                    {
                        case EnumTipoServicioNST.milenia:
                            EntMileniaSeleccionada oEntMileniaSeleccionada = new EntMileniaSeleccionada();
                            oEntMileniaSeleccionada.SKU = lstEntDetalleVentaBaseNST[d].lstEntServicioSeleccionadoNST[m].Sku;
                            oEntMileniaSeleccionada.Sobreprecio = lstEntDetalleVentaBaseNST[d].lstEntServicioSeleccionadoNST[m].montoSobreprecio;
                            oEntMileniaSeleccionada.SKUSpecified = oEntMileniaSeleccionada.SobreprecioSpecified = true;
                            oDetalleVentaUniticket.lstMileniasSeleccionadas[m] = oEntMileniaSeleccionada;
                            break;
                        case EnumTipoServicioNST.seguroDanios:
                            string comp = string.Empty;
                            oDetalleVentaUniticket.lstAtributos = new Atributo[5];
                            Atributo oAtributo = new Atributo();
                            oAtributo.Key = "skuPolizaMoto";
                            oAtributo.Value = lstEntDetalleVentaBaseNST[d].lstEntServicioSeleccionadoNST[m].Sku.ToString();
                            oDetalleVentaUniticket.lstAtributos[0] = oAtributo;
                            Atributo oAtributo2 = new Atributo();
                            oAtributo2.Key = "SeguroMotoSeleccionado";

                            if (lstEntDetalleVentaBaseNST[d].lstEntServicioSeleccionadoNST[m].eTipoCobertura == EnumTipoCobertura.sinSeguro)
                                comp = "dedaños";

                            oAtributo2.Value = lstEntDetalleVentaBaseNST[d].lstEntServicioSeleccionadoNST[m].eTipoCobertura.ToString().ToLower() + comp;
                            oDetalleVentaUniticket.lstAtributos[1] = oAtributo2;
                            Atributo oAtributo3 = new Atributo();
                            oAtributo3.Key = "precioPolizaMoto";
                            oAtributo3.Value = lstEntDetalleVentaBaseNST[d].lstEntServicioSeleccionadoNST[m].precioServicio.ToString();
                            oDetalleVentaUniticket.lstAtributos[2] = oAtributo3;

                            Atributo oAtributo4 = new Atributo();
                            oAtributo4.Key = "sobreprecioPolizaMoto";
                            oAtributo4.Value = lstEntDetalleVentaBaseNST[d].lstEntServicioSeleccionadoNST[m].montoSobreprecio.ToString();
                            oDetalleVentaUniticket.lstAtributos[3] = oAtributo4;

                            //Se guarda el uso del seguro
                            Atributo oAtributo5 = new Atributo();
                            oAtributo5.Key = "usoSeguro";
                            oAtributo5.Value = lstEntDetalleVentaBaseNST[d].lstEntServicioSeleccionadoNST[m].usoSeguro.ToString().ToLower();
                            oDetalleVentaUniticket.lstAtributos[4] = oAtributo5;

                            oDetalleVentaUniticket.lstMileniasSeleccionadas[m] = new EntMileniaSeleccionada();
                            break;
                        default:
                            oDetalleVentaUniticket.lstMileniasSeleccionadas[m] = new EntMileniaSeleccionada();
                            break;
                    }
                }
                for (int s = 0; s < lstEntDetalleVentaBaseNST[d].lstSeries.Count; s++)
                {
                    EntSeries oEntSeries = new EntSeries();
                    oEntSeries.SERIE = lstEntDetalleVentaBaseNST[d].lstSeries[s].serie;
                    oEntSeries.Sku = lstEntDetalleVentaBaseNST[d].lstSeries[s].sku;
                    oEntSeries.aplicaDescuentoSerie = lstEntDetalleVentaBaseNST[d].lstSeries[s].aplicaDescuentoSerie;
                    oEntSeries.aplicaDescuentoSerieSpecified = oEntSeries.fiTirSpecified = oEntSeries.NegocioSpecified = oEntSeries.SkuSpecified = true;

                    if (this.esTelefonia)
                    {
                        oEntSeries.CUENTAIUS = lstEntDetalleVentaBaseNST[d].lstSeries[s].CUENTAIUS;
                        oEntSeries.DN = lstEntDetalleVentaBaseNST[d].lstSeries[s].DN;
                        oEntSeries.ESPRINC = lstEntDetalleVentaBaseNST[d].lstSeries[s].ESPRINC;
                        oEntSeries.fiTir = lstEntDetalleVentaBaseNST[d].lstSeries[s].fiTir;
                        oEntSeries.ICCID = lstEntDetalleVentaBaseNST[d].lstSeries[s].ICCID;
                        oEntSeries.IMEI = lstEntDetalleVentaBaseNST[d].lstSeries[s].IMEI;
                        oEntSeries.MIN = lstEntDetalleVentaBaseNST[d].lstSeries[s].MIN;
                        oEntSeries.Negocio = lstEntDetalleVentaBaseNST[d].lstSeries[s].Negocio;
                        oEntSeries.SID = lstEntDetalleVentaBaseNST[d].lstSeries[s].SID;
                        oEntSeries.STATUS = lstEntDetalleVentaBaseNST[d].lstSeries[s].STATUS;
                        oEntSeries.Telefono = lstEntDetalleVentaBaseNST[d].lstSeries[s].Telefono;
                        if ((oEntSeries.Negocio == 11 || oEntSeries.Negocio == 47) && this.esSurtTelefonia)
                            this.esSurtTelefonia = false;
                    }
                    oDetalleVentaUniticket.lstSeries[s] = oEntSeries;
                }
                for (int p = 0; p < lstEntDetalleVentaBaseNST[d].lstEntPromocionAplicadaNST.Count; p++)
                {
                    PromocionAplicadaBase oPromocionAplicadaBase = new PromocionAplicadaBase();
                    oPromocionAplicadaBase.cantidad = 1;
                    oPromocionAplicadaBase.Descripcion = lstEntDetalleVentaBaseNST[d].lstEntPromocionAplicadaNST[p].descripcion;
                    oPromocionAplicadaBase.eTipoPromocion = (EnumTipoPromocion)Enum.Parse(typeof(EnumTipoPromocion), lstEntDetalleVentaBaseNST[d].lstEntPromocionAplicadaNST[p].eTipoPromocion.ToString());
                    //oPromocionAplicadaBase.eTipoPromocion = (EnumTipoPromocion)Convert.ToInt32(lstEntDetalleVentaBaseNST[d].lstEntPromocionAplicadaNST[p].eTipoPromocion);
                    oPromocionAplicadaBase.MontoOtorgado = lstEntDetalleVentaBaseNST[d].lstEntPromocionAplicadaNST[p].montoOtorgado;
                    oPromocionAplicadaBase.oConvivencia = new Convivencia();
                    oPromocionAplicadaBase.oConvivencia.Milenia = oPromocionAplicadaBase.oConvivencia.MileniaSpecified = true;
                    oPromocionAplicadaBase.oConvivencia.OtrasPromociones = oPromocionAplicadaBase.oConvivencia.OtrasPromocionesSpecified = true;
                    oPromocionAplicadaBase.oConvivencia.OtrosProductos = oPromocionAplicadaBase.oConvivencia.OtrosProductosSpecified = true;
                    oPromocionAplicadaBase.oConvivencia.Seguro = oPromocionAplicadaBase.oConvivencia.SeguroSpecified = true;
                    oPromocionAplicadaBase.oConvivencia.CantidadMaxima = 999999;
                    oPromocionAplicadaBase.oConvivencia.CantidadMaximaSpecified = true;
                    oPromocionAplicadaBase.PorcentajeOtorgado = lstEntDetalleVentaBaseNST[d].lstEntPromocionAplicadaNST[p].porcentajeOtorgado;
                    oPromocionAplicadaBase.PromocionId = lstEntDetalleVentaBaseNST[d].lstEntPromocionAplicadaNST[p].promocionId;
                    oPromocionAplicadaBase.SkuRegalo = lstEntDetalleVentaBaseNST[d].lstEntPromocionAplicadaNST[p].skuRegalo;
                    oPromocionAplicadaBase.AplicarDescuentoMonedero = lstEntDetalleVentaBaseNST[d].lstEntPromocionAplicadaNST[p].aplicarDescuentoMonedero;
                    oPromocionAplicadaBase.AplicarDescuentoMonederoSpecified = oPromocionAplicadaBase.cantidadSpecified = oPromocionAplicadaBase.eTipoPromocionSpecified = oPromocionAplicadaBase.MontoEngancheSpecified = oPromocionAplicadaBase.MontoOtorgadoSpecified = oPromocionAplicadaBase.MultiplicidadSpecified = oPromocionAplicadaBase.PorcentajeOtorgadoSpecified = oPromocionAplicadaBase.PromocionIdSpecified = oPromocionAplicadaBase.SkuRegaloSpecified = true;

                    oDetalleVentaUniticket.oPromocionAplicada[p] = oPromocionAplicadaBase;
                }
                oDetalleVentaUniticket.CantidadSpecified = oDetalleVentaUniticket.DescuentoEspecialSpecified = oDetalleVentaUniticket.DescuentoSpecified = oDetalleVentaUniticket.fnDesctoMktSpecified = oDetalleVentaUniticket.IdPlanSpecified =
                    oDetalleVentaUniticket.MecanicaSpecified = oDetalleVentaUniticket.MontoEngancheSpecified = oDetalleVentaUniticket.MontoSobrePrecioSpecified = oDetalleVentaUniticket.montoVariableTarjetaSpecified = oDetalleVentaUniticket.NegocioPlanSpecified =
                    oDetalleVentaUniticket.PrecioListaSpecified = oDetalleVentaUniticket.PrecioMostrarSpecified = oDetalleVentaUniticket.SKUMileniaSpecified = oDetalleVentaUniticket.SKUSpecified = oDetalleVentaUniticket.SobreprecioMileniaSpecified = oDetalleVentaUniticket.TipoPrecioSpecified = true;
                lstEntMarcadoVentaActual[0].ListaDetalleVenta[d] = oDetalleVentaUniticket;
            }
            lstEntMarcadoVentaActual[0].lstAtributos = new Atributo[2];
            Atributo oAtributoSurt = new Atributo();
            oAtributoSurt.Key = "SurtimientoVta";
            if (this.esTelefonia && this.esSurtTelefonia)
                oAtributoSurt.Value = "true";
            else
                oAtributoSurt.Value = "false";
            Atributo oAtributoVenta = new Atributo();
            oAtributoVenta.Key = "idVenta";
            oAtributoVenta.Value = "1";
            lstEntMarcadoVentaActual[0].lstAtributos[0] = oAtributoSurt;
            lstEntMarcadoVentaActual[0].lstAtributos[1] = oAtributoVenta;

            lstEntMarcadoVentaActual[0].objCredito = this.CrearCredito(oEntInfoPlazoNST, oEntAccionesCreditoNST);
            lstEntMarcadoVentaActual[0].oEntAvisameIpad = this.CrearEntAvisameIpad();
            lstEntMarcadoVentaActual[0].oEntPromocionPolicia = this.CrearEntPromocionPolicia();
            lstEntMarcadoVentaActual[0].oEntTarjetaAzteca = this.CrearEntTarjetaAzteca();
            lstEntMarcadoVentaActual[0].precioTotal = montoTotalVenta;
            lstEntMarcadoVentaActual[0].tipoVenta = eTipoVenta;
            lstEntMarcadoVentaActual[0].ImporteTotalOperacion = montoTotalVenta;
            lstEntMarcadoVentaActual[0].ImporteEfectivo = montoTotalEfectivo;
            lstEntMarcadoVentaActual[0].ImporteEfectivoSpecified = lstEntMarcadoVentaActual[0].ImporteTotalOperacionSpecified = lstEntMarcadoVentaActual[0].precioTotalSpecified = lstEntMarcadoVentaActual[0].tipoVentaSpecified = true;
            return lstEntMarcadoVentaActual;
        }

        private EntRespuestaPresupuestoNST CreaRespuestaGenerarPresupuesto(ResultadoVtaUniticket oResultadoVtaUniticket)
        {
            EntRespuestaPresupuestoNST oEntRespuestaPresupuestoNST = new EntRespuestaPresupuestoNST();
            switch (oResultadoVtaUniticket.TipoRespuesta)
            {
                case EnumTipoError.Error:
                    oEntRespuestaPresupuestoNST.oEntRespuestaNST.eTipoError = EnumTipoErrorNST.Error;
                    break;
                case EnumTipoError.ErrorRecompra:
                    oEntRespuestaPresupuestoNST.oEntRespuestaNST.eTipoError = EnumTipoErrorNST.ErrorRecompra;
                    break;
                case EnumTipoError.MensajeRecompra:
                    oEntRespuestaPresupuestoNST.oEntRespuestaNST.eTipoError = EnumTipoErrorNST.MensajeRecompra;
                    break;
                case EnumTipoError.MsnRecompraRapidoyFacil:
                    oEntRespuestaPresupuestoNST.oEntRespuestaNST.eTipoError = EnumTipoErrorNST.MsnRecompraRapidoyFacil;
                    break;
                case EnumTipoError.MsnRecompraVentaSinEnganche:
                    oEntRespuestaPresupuestoNST.oEntRespuestaNST.eTipoError = EnumTipoErrorNST.MsnRecompraVentaSinEnganche;
                    break;
                case EnumTipoError.SinError:
                    oEntRespuestaPresupuestoNST.oEntRespuestaNST.eTipoError = EnumTipoErrorNST.SinError;
                    break;
                case EnumTipoError.Warning:
                    oEntRespuestaPresupuestoNST.oEntRespuestaNST.eTipoError = EnumTipoErrorNST.Warning;
                    break;
            }
            oEntRespuestaPresupuestoNST.oEntRespuestaNST.mensajeError = oResultadoVtaUniticket.Mensaje;
            if (oEntRespuestaPresupuestoNST.oEntRespuestaNST.eTipoError == EnumTipoErrorNST.SinError)
                oEntRespuestaPresupuestoNST.idPresupuesto = oResultadoVtaUniticket.lstVentas[0].FolioPresupuesto;

            return oEntRespuestaPresupuestoNST;
        }

        private EntRespuestaVentaNST CreaRespuestaGenerarMarcarySurtir(ResultadoVtaUniticket oResultadoVtaUniticket)
        {
            EntRespuestaVentaNST oEntRespuestaVentaNST = new EntRespuestaVentaNST();
            switch (oResultadoVtaUniticket.TipoRespuesta)
            {
                case EnumTipoError.Error:
                    oEntRespuestaVentaNST.oEntRespuestaNST.eTipoError = EnumTipoErrorNST.Error;
                    break;
                case EnumTipoError.ErrorRecompra:
                    oEntRespuestaVentaNST.oEntRespuestaNST.eTipoError = EnumTipoErrorNST.ErrorRecompra;
                    break;
                case EnumTipoError.MensajeRecompra:
                    oEntRespuestaVentaNST.oEntRespuestaNST.eTipoError = EnumTipoErrorNST.MensajeRecompra;
                    break;
                case EnumTipoError.MsnRecompraRapidoyFacil:
                    oEntRespuestaVentaNST.oEntRespuestaNST.eTipoError = EnumTipoErrorNST.MsnRecompraRapidoyFacil;
                    break;
                case EnumTipoError.MsnRecompraVentaSinEnganche:
                    oEntRespuestaVentaNST.oEntRespuestaNST.eTipoError = EnumTipoErrorNST.MsnRecompraVentaSinEnganche;
                    break;
                case EnumTipoError.SinError:
                    oEntRespuestaVentaNST.oEntRespuestaNST.eTipoError = EnumTipoErrorNST.SinError;
                    break;
                case EnumTipoError.Warning:
                    oEntRespuestaVentaNST.oEntRespuestaNST.eTipoError = EnumTipoErrorNST.Warning;
                    break;
            }
            oEntRespuestaVentaNST.oEntRespuestaNST.mensajeError = oResultadoVtaUniticket.Mensaje;
            if (oEntRespuestaVentaNST.oEntRespuestaNST.eTipoError == EnumTipoErrorNST.SinError)
            {
                oEntRespuestaVentaNST.idPresupuesto = oResultadoVtaUniticket.lstVentas[0].FolioPresupuesto;
                oEntRespuestaVentaNST.idPedido = oResultadoVtaUniticket.lstVentas[0].IdPedido;
                oEntRespuestaVentaNST.idUniticket = Convert.ToInt32(oResultadoVtaUniticket.IdFolioUniticket);
                if (oResultadoVtaUniticket.lstVentas[0].NumOperacionSurtimiento > 0)
                    oEntRespuestaVentaNST.surtCorrecto = true;
            }

            return oEntRespuestaVentaNST;
        }

        private bool ValidaMSI(string sku)
        {
            WCFServicioTienda wsWCFServicioTienda = new WCFServicioTienda();
            EntPromocionesPost oEntPromocionesPost = wsWCFServicioTienda.ValidarVentaMSI(0, true, sku);
            if (oEntPromocionesPost.enumPromocionPost == EnumPromocionPostCotizador.MensajePromocion)
                return true;
            else
                return false;
        }

        private void ModificaTipoAgregado(List<EntDetalleVentaBaseNST> lstEntDetalleVentaBaseNST, List<EntDetalleVentaResNST> lstEntDetalleVentaResNST)
        {
            for (int i = 0; i < lstEntDetalleVentaBaseNST.Count; i++)
            {
                for (int j = 0; j < lstEntDetalleVentaResNST.Count; j++)
                {
                    if (lstEntDetalleVentaBaseNST[i].SKU == lstEntDetalleVentaResNST[j].SKU)
                    {
                        lstEntDetalleVentaResNST[j].eTipoAgregadoNST = lstEntDetalleVentaBaseNST[i].eTipoAgregadoNST;
                        lstEntDetalleVentaResNST[j].skuAgregaSugerido = lstEntDetalleVentaBaseNST[i].skuAgregaSugerido;
                        if (lstEntDetalleVentaBaseNST[i].oEntPlanNST != null)
                            lstEntDetalleVentaResNST[j].oEntPlanNST.eTipoPlan = lstEntDetalleVentaBaseNST[i].oEntPlanNST.eTipoPlan;
                        if (lstEntDetalleVentaBaseNST[i].eTipoProductoNST == EnumTipoProductoNST.preplan ||
                            lstEntDetalleVentaBaseNST[i].eTipoProductoNST == EnumTipoProductoNST.chippreplan)
                            lstEntDetalleVentaResNST[j].eTipoProductoNST = lstEntDetalleVentaBaseNST[i].eTipoProductoNST;
                        break;
                    }
                }
            }
        }

        private decimal CalculaTotalComplementos(List<EntDetalleVentaResNST> lstEntDetalleVentaResNST)
        {
            decimal totComp = 0;
            for (int j = 0; j < lstEntDetalleVentaResNST.Count; j++)
            {
                if (lstEntDetalleVentaResNST[j].eTipoAgregadoNST != EnumTipoAgregadoNST.Carrito)
                    totComp += (lstEntDetalleVentaResNST[j].precioLista - lstEntDetalleVentaResNST[j].montoDescuento) * lstEntDetalleVentaResNST[j].Cantidad;
            }
            return totComp;
        }

        private decimal ObtenerPrecioProducto(double sku)
        {

            Elektra.Negocio.Entidades.Producto.EntProducto oEntProducto = new Elektra.Negocio.Entidades.Producto.EntProducto();
            oEntProducto.BeginObject(sku);
            return Convert.ToDecimal(oEntProducto.PrecioLista);
        }

        /*private void ObtenerDatosProducto(ref DetalleVentaBonoNST oDetalleVentaBonoNST)
        {
            DetalleVentaBono ProductosBono = new DetalleVentaBono();
            this.ObtenerDatosProducto(ref oDetalleVentaBonoNST, ref ProductosBono);
        }

        private void ObtenerDatosProducto(ref DetalleVentaBono oDetalleVentaBonoNST)
        {
            DetalleVentaBonoNST ProductosBonoNST = new DetalleVentaBonoNST();
            this.ObtenerDatosProducto(ref ProductosBonoNST, ref oDetalleVentaBonoNST);
        }

        private void ObtenerDatosProducto(ref DetalleVentaBonoNST oDetalleVentaBonoNST, ref DetalleVentaBono oDetalleVentaBono)
        {
            int sku = 0;

            if (oDetalleVentaBonoNST != null && oDetalleVentaBonoNST.SKU > 0)
                sku = oDetalleVentaBonoNST.SKU;
            else
                sku = oDetalleVentaBono.SKU;
            
            Elektra.Negocio.Entidades.Producto.EntProducto oEntProducto = new Elektra.Negocio.Entidades.Producto.EntProducto();            
            oEntProducto.BeginObject(sku);

            if (oDetalleVentaBonoNST != null && oDetalleVentaBonoNST.SKU > 0)
            {
                oDetalleVentaBonoNST.Descripcion = oEntProducto.Descripcion;
                oDetalleVentaBonoNST.PrecioLista = decimal.Parse(oEntProducto.PrecioLista.ToString());
            }
            else
            {
                oDetalleVentaBono.Descripcion = oEntProducto.Descripcion;
                oDetalleVentaBono.PrecioLista = decimal.Parse(oEntProducto.PrecioLista.ToString());
            }

        }
        */
        private Credito CrearCredito(EntInfoPlazoNST oEntInfoPlazoNST, EntAccionesCreditoNST oEntAccionesCreditoNST)
        {
            Credito oCredito = new Credito();
            oCredito.AbonoMinimoNormalSpecified = oCredito.AbonoMinimoPlazoSpecified = oCredito.AbonoMinimoPorcentagePPSpecified =
                oCredito.AbonoMinimoSpecified = oCredito.AbonoMinimoTasaSpecified = oCredito.AbonoMinimoUltimoSpecified =
                oCredito.AbonoPagoPuntualPlanSpecified = oCredito.AbonoPagoPuntualSpecified = oCredito.AbonoPlanSpecified =
                oCredito.AbonoSpecified = oCredito.EsCreditoRapidoyFacilSpecified = oCredito.EsVentaConEngancheCeroSpecified =
                oCredito.MontoRentasAnticipadasSpecified = oCredito.NumeroPlazoSpecified = oCredito.PagoSemanalPlanSpecified =
                oCredito.PeriodoSpecified = oCredito.PorcentajePagoPuntualSpecified = oCredito.TasaSpecified =
                oCredito.UltimoAbonoPlanSpecified = oCredito.UltimoAbonoSpecified = oCredito.UltPagoSemanalPlanSpecified = true;
            if (oEntInfoPlazoNST != null)
            {
                oCredito.Abono = oEntInfoPlazoNST.abono;
                oCredito.AbonoMinimo = oEntInfoPlazoNST.aMinimo;
                oCredito.AbonoMinimoNormal = oEntInfoPlazoNST.aMinimoNormal;
                oCredito.AbonoMinimoPlazo = oEntInfoPlazoNST.aMinimoPlazo;
                oCredito.AbonoMinimoPorcentagePP = oEntInfoPlazoNST.aMinimoPorcentajePP;
                oCredito.AbonoMinimoTasa = oEntInfoPlazoNST.aMinimoTasa;
                oCredito.AbonoMinimoUltimo = oEntInfoPlazoNST.aMinimoUltimo;
                oCredito.AbonoPagoPuntual = oEntInfoPlazoNST.abonoPuntual;
                oCredito.NumeroPlazo = oEntInfoPlazoNST.plazo;
                oCredito.PorcentajePagoPuntual = oEntInfoPlazoNST.porcentajeAbonoPuntual;
                oCredito.Tasa = oEntInfoPlazoNST.tasa;
                oCredito.UltimoAbono = oEntInfoPlazoNST.ultimoAbono;
                oCredito.AbonoPlan = oEntInfoPlazoNST.AbonoPlan;
                oCredito.UltimoAbonoPlan = oEntInfoPlazoNST.UltimoAbonoPlan;
                oCredito.InfoEscalon = oEntInfoPlazoNST.InfoEscalon;

                switch (oEntInfoPlazoNST.ePeriodoNST)
                {
                    case EnumPeriodoNST.Semanal:
                        oCredito.Periodo = EnumPeriodos.Semanal;
                        break;
                    case EnumPeriodoNST.Quincenal:
                        oCredito.Periodo = EnumPeriodos.Quincenal;
                        break;
                    case EnumPeriodoNST.QuinceTreinta:
                        oCredito.Periodo = EnumPeriodos.QuinceTreinta;
                        break;
                    case EnumPeriodoNST.QuincenalN:
                        oCredito.Periodo = EnumPeriodos.QuincenalN;
                        break;
                    case EnumPeriodoNST.MensualN:
                        oCredito.Periodo = EnumPeriodos.MensualN;
                        break;
                }

                if (oEntAccionesCreditoNST != null)
                {
                    oCredito.EsCreditoRapidoyFacil = oEntAccionesCreditoNST.esCreditoRyF;
                    oCredito.EsVentaConEngancheCero = oEntAccionesCreditoNST.esVentaEngCero;
                }
            }
            return oCredito;
        }

        private EntAvisameIpad CrearEntAvisameIpad()
        {
            EntAvisameIpad oEntAvisameIpad = new EntAvisameIpad();
            oEntAvisameIpad.LstReferencias = new EntReferenciaAvisameIpad[0];
            oEntAvisameIpad.AplicaAvisameSpecified = oEntAvisameIpad.IdVentaOtorgaSpecified = oEntAvisameIpad.MontoAvisameSpecified =
                oEntAvisameIpad.ParametroEncendidoSpecified = oEntAvisameIpad.PresupuestoAvisameSpecified =
                oEntAvisameIpad.PresupuestoBaseSpecified = oEntAvisameIpad.SkuAvisameSpecified = true;
            return oEntAvisameIpad;
        }

        private EntEntregaDomIpad CrearEntEntregaDomIpad(EntClienteEntregaDomicilioNST oEntClienteEntregaDomicilioNST)
        {
            EntEntregaDomIpad oEntEntregaDomIpad = new EntEntregaDomIpad();
            oEntEntregaDomIpad.entregaCalculadaSpecified = oEntEntregaDomIpad.mecanicaSpecified =
                oEntEntregaDomIpad.precioEntregaSpecified = oEntEntregaDomIpad.skuSpecified =
                oEntEntregaDomIpad.sobreprecioEntregaSpecified = true;
            if (oEntClienteEntregaDomicilioNST != null)
            {
                oEntEntregaDomIpad.entregaCalculada = oEntClienteEntregaDomicilioNST.entregaCalculada;
                oEntEntregaDomIpad.mecanica = oEntClienteEntregaDomicilioNST.mecanica;
                oEntEntregaDomIpad.precioEntrega = oEntClienteEntregaDomicilioNST.precioEntrega;
                oEntEntregaDomIpad.sku = oEntClienteEntregaDomicilioNST.sku;
                oEntEntregaDomIpad.sobreprecioEntrega = oEntClienteEntregaDomicilioNST.sobreprecioEntrega;
            }
            return oEntEntregaDomIpad;
        }

        private EntPromocionPolicia CrearEntPromocionPolicia()
        {
            EntPromocionPolicia oEntPromocionPolicia = new EntPromocionPolicia();
            oEntPromocionPolicia.credencial = oEntPromocionPolicia.Mensaje = oEntPromocionPolicia.nombreCompleto = string.Empty;
            oEntPromocionPolicia.ExisteErrorSpecified = oEntPromocionPolicia.idPromocionSpecified =
                oEntPromocionPolicia.numeroVentaSpecified = oEntPromocionPolicia.validaHuellaSpecified = true;
            return oEntPromocionPolicia;
        }

        private EntTarjetaAzteca CrearEntTarjetaAzteca()
        {
            EntTarjetaAzteca oEntTarjetaAzteca = new EntTarjetaAzteca();
            oEntTarjetaAzteca.noTarjeta = string.Empty;
            oEntTarjetaAzteca.porcPromocionSpecified = oEntTarjetaAzteca.preguntaRealizadaSpecified = true;
            return oEntTarjetaAzteca;
        }

        private ClienteEntregaDomicilio CrearClienteEntregaDomicilio(EntClienteEntregaDomicilioNST oEntClienteEntregaDomicilioNST)
        {
            ClienteEntregaDomicilio oClienteEntregaDomicilio = new ClienteEntregaDomicilio();
            oClienteEntregaDomicilio.esMismoClienteSpecified = oClienteEntregaDomicilio.esNuevoPV = oClienteEntregaDomicilio.esNuevoPVSpecified = true;
            if (oEntClienteEntregaDomicilioNST != null)
            {
                oClienteEntregaDomicilio.apMaterno = oEntClienteEntregaDomicilioNST.apMaterno;
                oClienteEntregaDomicilio.apPaterno = oEntClienteEntregaDomicilioNST.apPaterno;
                oClienteEntregaDomicilio.calle = oEntClienteEntregaDomicilioNST.calle;
                oClienteEntregaDomicilio.calleAtras = oEntClienteEntregaDomicilioNST.calleAtras;
                oClienteEntregaDomicilio.calleDerecha = oEntClienteEntregaDomicilioNST.calleDerecha;
                oClienteEntregaDomicilio.calleFrente = oEntClienteEntregaDomicilioNST.calleFrente;
                oClienteEntregaDomicilio.calleIzquierda = oEntClienteEntregaDomicilioNST.calleIzquierda;
                oClienteEntregaDomicilio.colonia = oEntClienteEntregaDomicilioNST.colonia;
                oClienteEntregaDomicilio.correoElectronico = oEntClienteEntregaDomicilioNST.correoElectronico;
                oClienteEntregaDomicilio.cp = oEntClienteEntregaDomicilioNST.cp;
                oClienteEntregaDomicilio.estado = oEntClienteEntregaDomicilioNST.estado;
                oClienteEntregaDomicilio.fechaJDA = oEntClienteEntregaDomicilioNST.fechaJDA;
                oClienteEntregaDomicilio.fechaModificada = oEntClienteEntregaDomicilioNST.fechaModificada;
                oClienteEntregaDomicilio.nombreCompleto = oEntClienteEntregaDomicilioNST.nombreCompleto;
                oClienteEntregaDomicilio.numeroExt = oEntClienteEntregaDomicilioNST.numeroExt;
                oClienteEntregaDomicilio.numeroInt = oEntClienteEntregaDomicilioNST.numeroInt;
                oClienteEntregaDomicilio.observaciones = oEntClienteEntregaDomicilioNST.observaciones;
                oClienteEntregaDomicilio.poblacion = oEntClienteEntregaDomicilioNST.poblacion;
                oClienteEntregaDomicilio.telCelular = oEntClienteEntregaDomicilioNST.telCelular;
                oClienteEntregaDomicilio.telefono = oEntClienteEntregaDomicilioNST.telefono;
            }
            return oClienteEntregaDomicilio;
        }


        public bool SeConsultaNuevoCredito(int Item, int SubItem)
        {
            string RespuestaCatalogo = string.Empty;
            int RespTem = 0;

            try
            {
                RespuestaCatalogo = this.RecuperaCatalogoGenerico(Item, SubItem);
                RespuestaCatalogo = RespuestaCatalogo.IndexOf(":") > 0 ? RespuestaCatalogo.Substring(RespuestaCatalogo.IndexOf(":") + 1) : RespTem.ToString();
                int Result = RespuestaCatalogo.Trim().Length > 0 ? Convert.ToInt32(RespuestaCatalogo.Trim()) : RespTem;
                return Convert.ToBoolean(Result);
            }
            catch (Exception ex)
            {
                return Convert.ToBoolean(RespTem);
            }
        }

        public string RecuperaCatalogoGenerico(int itemId, int subItemId)
        {
            EntCatalogos catalogo = new EntCatalogos();
            DataSet dsCatalogo = catalogo.ObtenerCatalogoGenericoMaestro(itemId, subItemId);

            if (dsCatalogo != null && dsCatalogo.Tables != null && dsCatalogo.Tables.Count > 0 && dsCatalogo.Tables[0].Rows.Count > 0)
            {
                return dsCatalogo.Tables[0].Rows[0]["fcCatDesc"].ToString();
            }
            return string.Empty;
        }

        private EntRespuestaCotizarVentasCredNST CrearEntRespuestaCotizarVentasCredNST(RespuestaCotizarVentas oRespuestaCotizarVentas, EntRespuestaNST oEntRespuestaNST, EntComplementosNST oEntComplementosNST)
        {
            EntRespuestaCotizarVentasCredNST oEntRespuestaCotizarVentasCredNST = new EntRespuestaCotizarVentasCredNST();
            bool aplicaFG = false;
            decimal totServicios = 0;

            if (oRespuestaCotizarVentas.ListaVentaActual != null && oRespuestaCotizarVentas.ListaVentaActual.Length > 0)
            {
                if (oRespuestaCotizarVentas.ListaVentaActual[0].lstAbonos != null && oRespuestaCotizarVentas.ListaVentaActual[0].lstAbonos.Length > 0)
                {
                    for (int p = 0; p < oRespuestaCotizarVentas.ListaVentaActual[0].lstAbonos.Length; p++)
                    {
                        if (oRespuestaCotizarVentas.ListaVentaActual[0].lstAbonos[p].Abono > 0)
                        {
                            EntInfoPlazoNST oEntInfoPlazoNST = new EntInfoPlazoNST();
                            oEntInfoPlazoNST.abono = oRespuestaCotizarVentas.ListaVentaActual[0].lstAbonos[p].Abono;
                            oEntInfoPlazoNST.abonoPuntual = oRespuestaCotizarVentas.ListaVentaActual[0].lstAbonos[p].AbonoPagoPuntual;
                            oEntInfoPlazoNST.aMinimo = oRespuestaCotizarVentas.ListaVentaActual[0].lstAbonos[p].AbonoMinimo;
                            oEntInfoPlazoNST.aMinimoNormal = oRespuestaCotizarVentas.ListaVentaActual[0].lstAbonos[p].AbonoMinimoNormal;
                            oEntInfoPlazoNST.aMinimoPlazo = oRespuestaCotizarVentas.ListaVentaActual[0].lstAbonos[p].AbonoMinimoPlazo;
                            oEntInfoPlazoNST.aMinimoPorcentajePP = oRespuestaCotizarVentas.ListaVentaActual[0].lstAbonos[p].AbonoMinimoPorcentagePP;
                            oEntInfoPlazoNST.aMinimoTasa = oRespuestaCotizarVentas.ListaVentaActual[0].lstAbonos[p].AbonoMinimoTasa;
                            oEntInfoPlazoNST.aMinimoUltimo = oRespuestaCotizarVentas.ListaVentaActual[0].lstAbonos[p].AbonoMinimoUltimo;
                            switch (oRespuestaCotizarVentas.ListaVentaActual[0].lstAbonos[p].Periodo)
                            {
                                case EnumPeriodos.Semanal:
                                    oEntInfoPlazoNST.ePeriodoNST = EnumPeriodoNST.Semanal;
                                    break;
                                case EnumPeriodos.Quincenal:
                                    oEntInfoPlazoNST.ePeriodoNST = EnumPeriodoNST.Quincenal;
                                    break;
                                case EnumPeriodos.QuincenalN:
                                    oEntInfoPlazoNST.ePeriodoNST = EnumPeriodoNST.QuincenalN;
                                    break;
                                case EnumPeriodos.MensualN:
                                    oEntInfoPlazoNST.ePeriodoNST = EnumPeriodoNST.MensualN;
                                    break;
                            }

                            oEntInfoPlazoNST.esSeleccionado = oRespuestaCotizarVentas.ListaVentaActual[0].lstAbonos[p].EsPlazoSeleccionado;
                            oEntInfoPlazoNST.plazo = oRespuestaCotizarVentas.ListaVentaActual[0].lstAbonos[p].NumeroPlazo;
                            oEntInfoPlazoNST.porcentajeAbonoPuntual = oRespuestaCotizarVentas.ListaVentaActual[0].lstAbonos[p].PorcentajePagoPuntual;
                            oEntInfoPlazoNST.tasa = oRespuestaCotizarVentas.ListaVentaActual[0].lstAbonos[p].Tasa;
                            oEntInfoPlazoNST.ultimoAbono = oRespuestaCotizarVentas.ListaVentaActual[0].lstAbonos[p].UltimoAbono;
                            oEntInfoPlazoNST.statusPlazo = oRespuestaCotizarVentas.ListaVentaActual[0].lstAbonos[p].StatusAbono;
                            oEntInfoPlazoNST.AbonoPlan = oRespuestaCotizarVentas.ListaVentaActual[0].lstAbonos[p].AbonoPlan;
                            oEntInfoPlazoNST.UltimoAbonoPlan = oRespuestaCotizarVentas.ListaVentaActual[0].lstAbonos[p].UltimoAbonoPlan;

                            oEntInfoPlazoNST.oEntInfoCATNST.CATPorcentajeNST = oRespuestaCotizarVentas.ListaVentaActual[0].lstAbonos[p].cat.CATPorcentaje;
                            oEntInfoPlazoNST.oEntInfoCATNST.TasaAnualSobreSaldosInsolutosNST = oRespuestaCotizarVentas.ListaVentaActual[0].lstAbonos[p].cat.TasaAnualSobreSaldosInsolutos;
                            oEntInfoPlazoNST.oEntInfoCATNST.TasaGlobalAnualNST = oRespuestaCotizarVentas.ListaVentaActual[0].lstAbonos[p].cat.TasaGlobalAnual;
                            oEntInfoPlazoNST.oEntInfoCATNST.TasaGlobalPeriodoNST = oRespuestaCotizarVentas.ListaVentaActual[0].lstAbonos[p].cat.TasaGlobalPeriodo;

                            oEntRespuestaCotizarVentasCredNST.lstEntInfoPlazoNST.Add(oEntInfoPlazoNST);
                        }
                    }
                }

                if (oRespuestaCotizarVentas.ListaVentaActual[0].CadenaProductos.Trim().Length > 0)
                    oEntRespuestaCotizarVentasCredNST.oEntComplementosNST.CadenaProductos = oRespuestaCotizarVentas.ListaVentaActual[0].CadenaProductos;

                if (oEntRespuestaCotizarVentasCredNST.lstEntInfoPlazoNST.Count > 0)
                {
                    if (oRespuestaCotizarVentas.ListaVentaActual[0].ListaDetalleVenta.Length > 0)
                    {
                        oEntRespuestaCotizarVentasCredNST.totalDeVentas = oRespuestaCotizarVentas.ListaVentaActual[0].TotalPrecioCredito;
                        oEntRespuestaCotizarVentasCredNST.montoTotalSobreprecio = oRespuestaCotizarVentas.ListaVentaActual[0].TotalSobreprecio;
                        oEntRespuestaCotizarVentasCredNST.montoFinanciar = Convert.ToDecimal(oRespuestaCotizarVentas.ListaVentaActual[0].TotalPrecio);
                    }

                    if (oEntComplementosNST != null && oEntComplementosNST.oEntClienteEntregaDomicilioNST != null)
                    {
                        oEntRespuestaCotizarVentasCredNST.oEntComplementosNST.oEntClienteEntregaDomicilioNST = oEntComplementosNST.oEntClienteEntregaDomicilioNST;
                        oEntRespuestaCotizarVentasCredNST.oEntComplementosNST.oEntClienteEntregaDomicilioNST.entregaCalculada = oRespuestaCotizarVentas.ListaVentaActual[0].oEntEntregaDomIpad.entregaCalculada;
                        oEntRespuestaCotizarVentasCredNST.oEntComplementosNST.oEntClienteEntregaDomicilioNST.mecanica = oRespuestaCotizarVentas.ListaVentaActual[0].oEntEntregaDomIpad.mecanica;
                        oEntRespuestaCotizarVentasCredNST.oEntComplementosNST.oEntClienteEntregaDomicilioNST.precioEntrega = oRespuestaCotizarVentas.ListaVentaActual[0].oEntEntregaDomIpad.precioEntrega;
                        oEntRespuestaCotizarVentasCredNST.oEntComplementosNST.oEntClienteEntregaDomicilioNST.sku = oRespuestaCotizarVentas.ListaVentaActual[0].oEntEntregaDomIpad.sku;
                        oEntRespuestaCotizarVentasCredNST.oEntComplementosNST.oEntClienteEntregaDomicilioNST.sobreprecioEntrega = oRespuestaCotizarVentas.ListaVentaActual[0].oEntEntregaDomIpad.sobreprecioEntrega;
                    }
                    if (oEntComplementosNST != null && oEntComplementosNST.oEntVentaRefacciones != null && oEntComplementosNST.oEntVentaRefacciones.DetalleRefacciones != null && oEntComplementosNST.oEntVentaRefacciones.DetalleRefacciones.Folio.Trim().Length > 0)
                        oEntRespuestaCotizarVentasCredNST.oEntComplementosNST.oEntVentaRefacciones = oEntComplementosNST.oEntVentaRefacciones;

                    oEntRespuestaCotizarVentasCredNST.totalVentaDescuentos = oRespuestaCotizarVentas.ListaVentaActual[0].TotalDescuentos;
                    oEntRespuestaCotizarVentasCredNST.TipoEsqPago = oRespuestaCotizarVentas.ListaVentaActual[0].TipoEsqPago;
                    oEntRespuestaCotizarVentasCredNST.AplicaMondederoLealtad = oRespuestaCotizarVentas.ListaVentaActual[0].AplicaMondederoLealtad;

                    if (oEntRespuestaNST.eTipoError == EnumTipoErrorNST.SinError)
                        oEntRespuestaCotizarVentasCredNST.oEntRespuestaNST.eTipoError = (EnumTipoErrorNST)Convert.ToInt32(oRespuestaCotizarVentas.ListaVentaActual[0].Respuesta);
                    else
                        oEntRespuestaCotizarVentasCredNST.oEntRespuestaNST.eTipoError = oEntRespuestaNST.eTipoError;

                    oEntRespuestaCotizarVentasCredNST.oEntRespuestaNST.mensajeError = oEntRespuestaNST.mensajeError + oRespuestaCotizarVentas.ListaVentaActual[0].Mensaje;
                    for (int i = 0; i < oRespuestaCotizarVentas.ListaVentaActual[0].ListaDetalleVenta.Length; i++)
                    {
                        bool agregaSeguroDanios = false;
                        EnumTipoCobertura tipoAux = EnumTipoCobertura.sinSeguro;
                        EnumUsoSeguro usoAux = EnumUsoSeguro.Particular;
                        int skuAux = 0;
                        decimal precioAux = 0, sobreprecioAux = 0, abonoServ = 0, abonoppServ = 0, uAbonoServ = 0;

                        EntDetalleVentaResNST oEntDetalleVentaResNST = new EntDetalleVentaResNST();
                        oEntDetalleVentaResNST.Cantidad = oRespuestaCotizarVentas.ListaVentaActual[0].ListaDetalleVenta[i].Cantidad;
                        oEntDetalleVentaResNST.descripcion = oRespuestaCotizarVentas.ListaVentaActual[0].ListaDetalleVenta[i].Descripcion.Trim();
                        oEntDetalleVentaResNST.montoDescuento = oRespuestaCotizarVentas.ListaVentaActual[0].ListaDetalleVenta[i].Descuento;
                        oEntDetalleVentaResNST.montoSobreprecio = oRespuestaCotizarVentas.ListaVentaActual[0].ListaDetalleVenta[i].MontoSobrePrecio;
                        oEntDetalleVentaResNST.montoEnganche = oRespuestaCotizarVentas.ListaVentaActual[0].ListaDetalleVenta[i].MontoEnganche;
                        oEntDetalleVentaResNST.mecanica = oRespuestaCotizarVentas.ListaVentaActual[0].ListaDetalleVenta[i].Mecanica;
                        oEntDetalleVentaResNST.precioLista = oRespuestaCotizarVentas.ListaVentaActual[0].ListaDetalleVenta[i].PrecioLista;
                        oEntDetalleVentaResNST.SKU = oRespuestaCotizarVentas.ListaVentaActual[0].ListaDetalleVenta[i].SKU;
                        oEntDetalleVentaResNST.aplicaMSI = this.ValidaMSI(oEntDetalleVentaResNST.SKU.ToString());
                        oEntDetalleVentaResNST.esLiquidacion = oRespuestaCotizarVentas.ListaVentaActual[0].ListaDetalleVenta[i].EsLiquidacion;
                        oEntDetalleVentaResNST.existencia = oRespuestaCotizarVentas.ListaVentaActual[0].ListaDetalleVenta[i].Existencia;
                        oEntDetalleVentaResNST.oEntPlanNST.IdPlan = oRespuestaCotizarVentas.ListaVentaActual[0].ListaDetalleVenta[i].IdPlan;
                        oEntDetalleVentaResNST.oEntPlanNST.NegocioPlan = oRespuestaCotizarVentas.ListaVentaActual[0].ListaDetalleVenta[i].NegocioPlan;
                        oEntDetalleVentaResNST.oEntPlanNST.DescripcionPlan = oRespuestaCotizarVentas.ListaVentaActual[0].ListaDetalleVenta[i].DescripcionPlan;
                        oEntDetalleVentaResNST.oEntPlanNST.DescripcionNegocio = oRespuestaCotizarVentas.ListaVentaActual[0].ListaDetalleVenta[i].DescripcionNegocioPlan;
                        oEntDetalleVentaResNST.eTipoAgregadoMileniaNST = (EnumTipoAgregadoMileniaNST)Convert.ToInt32(oRespuestaCotizarVentas.ListaVentaActual[0].ListaDetalleVenta[i].eTipoAgregadoMilenia);
                        oEntDetalleVentaResNST.pagoPuntualDetalle = oRespuestaCotizarVentas.ListaVentaActual[0].ListaDetalleVenta[i].pagoPuntualDetalle;
                        oEntDetalleVentaResNST.abonoProducto = oRespuestaCotizarVentas.ListaVentaActual[0].ListaDetalleVenta[i].AbonoProducto;
                        oEntDetalleVentaResNST.abonoPPProducto = oRespuestaCotizarVentas.ListaVentaActual[0].ListaDetalleVenta[i].AbonoPPProducto;
                        oEntDetalleVentaResNST.ultabonoProducto = oRespuestaCotizarVentas.ListaVentaActual[0].ListaDetalleVenta[i].UltAbonoProducto;
                        oEntDetalleVentaResNST.PrecioSugerido = oRespuestaCotizarVentas.ListaVentaActual[0].ListaDetalleVenta[i].PrecioSugerido;
                        oEntDetalleVentaResNST.tipoBloqueo = oRespuestaCotizarVentas.ListaVentaActual[0].ListaDetalleVenta[i].TipoBloqueo;
                        oEntDetalleVentaResNST.tienePaquetes = ValidaSiTienePaquete(oRespuestaCotizarVentas.ListaVentaActual[0].ListaDetalleVenta[i].SKU);
                        oEntDetalleVentaResNST.DeptoProd = oRespuestaCotizarVentas.ListaVentaActual[0].ListaDetalleVenta[i].DeptoProd;
                        switch (oRespuestaCotizarVentas.ListaVentaActual[0].ListaDetalleVenta[i].TipoProducto)
                        {
                            case EnumTipoProductos.Comercio:
                                oEntDetalleVentaResNST.eTipoProductoNST = EnumTipoProductoNST.mercancias;
                                break;
                            case EnumTipoProductos.MotosOtraMarca:
                                oEntDetalleVentaResNST.eTipoProductoNST = EnumTipoProductoNST.MotosOtraMarca;
                                break;
                            case EnumTipoProductos.MotosServicioPrepagadoItk:
                                oEntDetalleVentaResNST.eTipoProductoNST = EnumTipoProductoNST.MotosServicioPrepagadoItk;
                                break;
                            case EnumTipoProductos.MotosConSerie:
                            case EnumTipoProductos.Motos:
                                oEntDetalleVentaResNST.eTipoProductoNST = EnumTipoProductoNST.motos;
                                break;
                            case EnumTipoProductos.Telefonia:
                            case EnumTipoProductos.EquipoTelefonia:
                                oEntDetalleVentaResNST.eTipoProductoNST = EnumTipoProductoNST.telefonia;
                                break;
                        }

                        if (oRespuestaCotizarVentas.ListaVentaActual[0].ListaDetalleVenta[i].lstOmitirPromociones != null && oRespuestaCotizarVentas.ListaVentaActual[0].ListaDetalleVenta[i].lstOmitirPromociones.Length > 0)
                        {
                            for (int p = 0; p < oRespuestaCotizarVentas.ListaVentaActual[0].ListaDetalleVenta[i].lstOmitirPromociones.Length; p++)
                            {
                                oEntDetalleVentaResNST.lstOmitirPromociones.Add(oRespuestaCotizarVentas.ListaVentaActual[0].ListaDetalleVenta[i].lstOmitirPromociones[p]);
                            }
                        }

                        if (!aplicaFG)
                            aplicaFG = new ManejadorPromocionesNST().ValidarFleteGratis(oRespuestaCotizarVentas.ListaVentaActual[0].ListaDetalleVenta[i].SKU);

                        for (int m = 0, max = oRespuestaCotizarVentas.ListaVentaActual[0].ListaDetalleVenta[i].lstMilenias.Length; m < max; m++)
                        {
                            EnumTipoServicioNST eTipoServicio = EnumTipoServicioNST.sinServicio;
                            EnumTipoCobertura eTipoCobertura = EnumTipoCobertura.sinSeguro;
                            EnumUsoSeguro eUsoSeguro = EnumUsoSeguro.Particular;
                            for (int a = 0; a < oRespuestaCotizarVentas.ListaVentaActual[0].ListaDetalleVenta[i].lstMilenias[m].lstAtributos.Length; a++)
                            {
                                if (oRespuestaCotizarVentas.ListaVentaActual[0].ListaDetalleVenta[i].lstMilenias[m].lstAtributos[a].Value.ToUpper() == "MILENIA")
                                    eTipoServicio = EnumTipoServicioNST.milenia;
                                else
                                    eTipoServicio = EnumTipoServicioNST.seguroDanios;
                            }
                            //Se recupera la informacion de la cobertura y el uso de los atributos del objeto recuperado
                            Atributo[] infoAtt_ = oRespuestaCotizarVentas.ListaVentaActual[0].ListaDetalleVenta[i].lstMilenias[m].lstAtributos;
                            if (infoAtt_ != null && infoAtt_.Count() > 0)
                            {
                                foreach (var obj_ in infoAtt_)
                                {
                                    if (obj_.Key == "SeguroMotoSeleccionado")
                                    {
                                        string[] cob_ = obj_.Value.Split(Convert.ToChar(":"));
                                        if (cob_ != null && cob_.Length > 1)
                                        {
                                            eTipoCobertura = (EnumTipoCobertura)Convert.ToInt32(cob_[0]); //cobertura que se guardo en el WS
                                            eUsoSeguro = (EnumUsoSeguro)Convert.ToInt32(cob_[1]); //Uso del seguro que se guardo en el WS
                                        }
                                    }
                                }
                            }
                            EntServicioNST oEntServicioNST = new EntServicioNST(oRespuestaCotizarVentas.ListaVentaActual[0].ListaDetalleVenta[i].lstMilenias[m].SKU,
                                                                                oRespuestaCotizarVentas.ListaVentaActual[0].ListaDetalleVenta[i].lstMilenias[m].Precio,
                                                                                oRespuestaCotizarVentas.ListaVentaActual[0].ListaDetalleVenta[i].lstMilenias[m].Descripcion,
                                                                                eTipoServicio,
                                                                                eTipoCobertura,
                                                                                eUsoSeguro);
                            oEntDetalleVentaResNST.lstMileniasDisponibles.Add(oEntServicioNST);
                        }

                        for (int ms = 0; ms < oRespuestaCotizarVentas.ListaVentaActual[0].ListaDetalleVenta[i].lstMileniasSeleccionadas.Length; ms++)
                        {
                            decimal precio = 0;
                            for (int mds = 0; mds < oRespuestaCotizarVentas.ListaVentaActual[0].ListaDetalleVenta[i].lstMilenias.Length; mds++)
                            {
                                if (oRespuestaCotizarVentas.ListaVentaActual[0].ListaDetalleVenta[i].lstMileniasSeleccionadas[ms].SKU ==
                                    oRespuestaCotizarVentas.ListaVentaActual[0].ListaDetalleVenta[i].lstMilenias[mds].SKU)
                                {
                                    precio = oRespuestaCotizarVentas.ListaVentaActual[0].ListaDetalleVenta[i].lstMilenias[mds].Precio;
                                    totServicios += precio;
                                    break;
                                }
                            }
                            EntServicioSeleccionadoNST oEntServicioSeleccionadoNST = new EntServicioSeleccionadoNST(EnumTipoServicioNST.milenia,
                                                                                                                    oRespuestaCotizarVentas.ListaVentaActual[0].ListaDetalleVenta[i].lstMileniasSeleccionadas[ms].SKU,
                                                                                                                    EnumTipoCobertura.sinSeguro,
                                                                                                                    precio,
                                                                                                                    oRespuestaCotizarVentas.ListaVentaActual[0].ListaDetalleVenta[i].lstMileniasSeleccionadas[ms].Sobreprecio,
                                                                                                                    EnumUsoSeguro.Particular,
                                                                                                                    oRespuestaCotizarVentas.ListaVentaActual[0].ListaDetalleVenta[i].lstMileniasSeleccionadas[ms].abonoProducto,
                                                                                                                    oRespuestaCotizarVentas.ListaVentaActual[0].ListaDetalleVenta[i].lstMileniasSeleccionadas[ms].abonoPPProducto,
                                                                                                                    oRespuestaCotizarVentas.ListaVentaActual[0].ListaDetalleVenta[i].lstMileniasSeleccionadas[ms].ultabonoProducto);
                            oEntDetalleVentaResNST.lstEntServicioSeleccionadoNST.Add(oEntServicioSeleccionadoNST);
                        }

                        for (int sd = 0, max = oRespuestaCotizarVentas.ListaVentaActual[0].ListaDetalleVenta[i].lstAtributos.Length; sd < max; sd++)
                        {
                            if (oRespuestaCotizarVentas.ListaVentaActual[0].ListaDetalleVenta[i].lstAtributos[sd].Key == "SeguroMotoSeleccionado")
                            {
                                string[] cobs_ = oRespuestaCotizarVentas.ListaVentaActual[0].ListaDetalleVenta[i].lstAtributos[sd].Value.Split(Convert.ToChar(":"));
                                if (cobs_ != null && cobs_.Length > 1)
                                {
                                    tipoAux = (EnumTipoCobertura)Convert.ToInt32(cobs_[0]);
                                    usoAux = (EnumUsoSeguro)Convert.ToInt32(cobs_[1]);
                                }
                                agregaSeguroDanios = true;
                            }

                            if (oRespuestaCotizarVentas.ListaVentaActual[0].ListaDetalleVenta[i].lstAtributos[sd].Key == "skuPolizaMoto")
                                skuAux = Convert.ToInt32(oRespuestaCotizarVentas.ListaVentaActual[0].ListaDetalleVenta[i].lstAtributos[sd].Value);

                            if (oRespuestaCotizarVentas.ListaVentaActual[0].ListaDetalleVenta[i].lstAtributos[sd].Key == "precioPolizaMoto")
                                precioAux = Convert.ToDecimal(oRespuestaCotizarVentas.ListaVentaActual[0].ListaDetalleVenta[i].lstAtributos[sd].Value);

                            if (oRespuestaCotizarVentas.ListaVentaActual[0].ListaDetalleVenta[i].lstAtributos[sd].Key == "sobreprecioPolizaMoto")
                                sobreprecioAux = Convert.ToDecimal(oRespuestaCotizarVentas.ListaVentaActual[0].ListaDetalleVenta[i].lstAtributos[sd].Value);

                            if (oRespuestaCotizarVentas.ListaVentaActual[0].ListaDetalleVenta[i].lstAtributos[sd].Key == "abonoProductoItalika")
                                abonoServ = Convert.ToDecimal(oRespuestaCotizarVentas.ListaVentaActual[0].ListaDetalleVenta[i].lstAtributos[sd].Value);

                            if (oRespuestaCotizarVentas.ListaVentaActual[0].ListaDetalleVenta[i].lstAtributos[sd].Key == "abonoPPProductoItalika")
                                abonoppServ = Convert.ToDecimal(oRespuestaCotizarVentas.ListaVentaActual[0].ListaDetalleVenta[i].lstAtributos[sd].Value);

                            if (oRespuestaCotizarVentas.ListaVentaActual[0].ListaDetalleVenta[i].lstAtributos[sd].Key == "ultabonoProductoItalika")
                                uAbonoServ = Convert.ToDecimal(oRespuestaCotizarVentas.ListaVentaActual[0].ListaDetalleVenta[i].lstAtributos[sd].Value);
                        }

                        if (agregaSeguroDanios)
                        {
                            for (int qm = 0; qm < oEntDetalleVentaResNST.lstMileniasDisponibles.Count; qm++)
                                if (oEntDetalleVentaResNST.lstMileniasDisponibles[qm].Descripcion.ToUpper() == "GARANTÍA DEL PROVEEDOR")
                                {
                                    oEntDetalleVentaResNST.lstMileniasDisponibles.Remove(oEntDetalleVentaResNST.lstMileniasDisponibles[qm]);
                                    break;
                                }

                            for (int ss = 0; ss < oEntDetalleVentaResNST.lstEntServicioSeleccionadoNST.Count; ss++)
                                if (oEntDetalleVentaResNST.lstEntServicioSeleccionadoNST[ss].eTipoServicio == EnumTipoServicioNST.milenia)
                                {
                                    oEntDetalleVentaResNST.lstEntServicioSeleccionadoNST.Remove(oEntDetalleVentaResNST.lstEntServicioSeleccionadoNST[ss]);
                                    ss--;
                                }

                            totServicios += precioAux;
                            oEntDetalleVentaResNST.lstEntServicioSeleccionadoNST.Add(new EntServicioSeleccionadoNST(EnumTipoServicioNST.seguroDanios, skuAux, tipoAux, precioAux, sobreprecioAux, usoAux, abonoServ, abonoppServ, uAbonoServ));
                        }

                        if (oEntDetalleVentaResNST.lstEntServicioSeleccionadoNST.Count == 0)
                            oEntDetalleVentaResNST.lstEntServicioSeleccionadoNST.Add(new EntServicioSeleccionadoNST());

                        if (oRespuestaCotizarVentas.ListaVentaActual[0].ListaDetalleVenta[i].lstSeriesValidas != null)
                        {
                            for (int s = 0; s < oRespuestaCotizarVentas.ListaVentaActual[0].ListaDetalleVenta[i].lstSeriesValidas.Length; s++)
                            {
                                EntSerieValidaNST oEntSerieValidaNST = new EntSerieValidaNST();
                                oEntSerieValidaNST.serie = oRespuestaCotizarVentas.ListaVentaActual[0].ListaDetalleVenta[i].lstSeriesValidas[s].serie;
                                oEntSerieValidaNST.porcentajeDesc = Convert.ToDecimal(oRespuestaCotizarVentas.ListaVentaActual[0].ListaDetalleVenta[i].lstSeriesValidas[s].porcentajeDesc);

                                oEntDetalleVentaResNST.lstEntSerieValidaNST.Add(oEntSerieValidaNST);
                            }
                        }
                        for (int ss = 0; ss < oRespuestaCotizarVentas.ListaVentaActual[0].ListaDetalleVenta[i].lstSeries.Length; ss++)
                        {
                            EntSerieNST oEntSerieNST = new EntSerieNST();
                            oEntSerieNST.aplicaDescuentoSerie = oRespuestaCotizarVentas.ListaVentaActual[0].ListaDetalleVenta[i].lstSeries[ss].aplicaDescuentoSerie;
                            oEntSerieNST.serie = oRespuestaCotizarVentas.ListaVentaActual[0].ListaDetalleVenta[i].lstSeries[ss].SERIE;
                            oEntSerieNST.Negocio = oRespuestaCotizarVentas.ListaVentaActual[0].ListaDetalleVenta[i].lstSeries[ss].Negocio;
                            for (int sd = 0; sd < oRespuestaCotizarVentas.ListaVentaActual[0].ListaDetalleVenta[i].lstAtributos.Length; sd++)
                            {
                                switch (oRespuestaCotizarVentas.ListaVentaActual[0].ListaDetalleVenta[i].lstAtributos[sd].Key)
                                {
                                    case "LongMaxSerie":
                                        oEntSerieNST.LongMaxSerie = Convert.ToInt32(oRespuestaCotizarVentas.ListaVentaActual[0].ListaDetalleVenta[i].lstAtributos[sd].Value);
                                        break;
                                    case "LongMaxIMEI":
                                        oEntSerieNST.LongMaxIMEI = Convert.ToInt32(oRespuestaCotizarVentas.ListaVentaActual[0].ListaDetalleVenta[i].lstAtributos[sd].Value);
                                        break;
                                    case "LongMaxChip":
                                        oEntSerieNST.LongMaxChip = Convert.ToInt32(oRespuestaCotizarVentas.ListaVentaActual[0].ListaDetalleVenta[i].lstAtributos[sd].Value);
                                        break;
                                }
                            }
                            oEntDetalleVentaResNST.lstSeries.Add(oEntSerieNST);
                        }

                        for (int p = 0; p < oRespuestaCotizarVentas.ListaVentaActual[0].ListaDetalleVenta[i].oPromocionAplicada.Length; p++)
                        {
                            EntPromocionAplicadaNST oEntPromocionAplicadaNST = new EntPromocionAplicadaNST();
                            oEntPromocionAplicadaNST.descripcion = oRespuestaCotizarVentas.ListaVentaActual[0].ListaDetalleVenta[i].oPromocionAplicada[p].Descripcion;
                            oEntPromocionAplicadaNST.montoOtorgado = oRespuestaCotizarVentas.ListaVentaActual[0].ListaDetalleVenta[i].oPromocionAplicada[p].MontoOtorgado;
                            oEntPromocionAplicadaNST.porcentajeOtorgado = oRespuestaCotizarVentas.ListaVentaActual[0].ListaDetalleVenta[i].oPromocionAplicada[p].PorcentajeOtorgado;
                            oEntPromocionAplicadaNST.promocionId = oRespuestaCotizarVentas.ListaVentaActual[0].ListaDetalleVenta[i].oPromocionAplicada[p].PromocionId;
                            oEntPromocionAplicadaNST.skuRegalo = oRespuestaCotizarVentas.ListaVentaActual[0].ListaDetalleVenta[i].oPromocionAplicada[p].SkuRegalo;
                            oEntPromocionAplicadaNST.montoDispDesc = oRespuestaCotizarVentas.ListaVentaActual[0].ListaDetalleVenta[i].oPromocionAplicada[p].MontoMaxDescEsp;
                            oEntPromocionAplicadaNST.NombreCompleto = oRespuestaCotizarVentas.ListaVentaActual[0].ListaDetalleVenta[i].oPromocionAplicada[p].NombreCompleto;
                            oEntPromocionAplicadaNST.DivisionFuerzas = oRespuestaCotizarVentas.ListaVentaActual[0].ListaDetalleVenta[i].oPromocionAplicada[p].DivisionFuerzas;
                            oEntPromocionAplicadaNST.eTipoPromocion = (EnumTipoPromocionNST)Enum.Parse(typeof(EnumTipoPromocionNST), oRespuestaCotizarVentas.ListaVentaActual[0].ListaDetalleVenta[i].oPromocionAplicada[p].eTipoPromocion.ToString());
                            //oEntPromocionAplicadaNST.eTipoPromocion = (EnumTipoPromocionNST)Convert.ToInt32(oRespuestaCotizarVentas.ListaVentaActual[0].ListaDetalleVenta[i].oPromocionAplicada[p].eTipoPromocion);
                            oEntPromocionAplicadaNST.aplicarDescuentoMonedero = oRespuestaCotizarVentas.ListaVentaActual[0].ListaDetalleVenta[i].oPromocionAplicada[p].AplicarDescuentoMonedero;
                            oEntPromocionAplicadaNST.folio = oRespuestaCotizarVentas.ListaVentaActual[0].ListaDetalleVenta[i].oPromocionAplicada[p].folioEspecial;
                            oEntPromocionAplicadaNST.programaInstitucional = oRespuestaCotizarVentas.ListaVentaActual[0].ListaDetalleVenta[i].oPromocionAplicada[p].programa;
                            oEntPromocionAplicadaNST.skuOtorga = oRespuestaCotizarVentas.ListaVentaActual[0].ListaDetalleVenta[i].oPromocionAplicada[p].SKU_Linea_Otorga;
                            oEntPromocionAplicadaNST.eTipoBonoNST = (EnumTipoBonoNST)Convert.ToInt32(oRespuestaCotizarVentas.ListaVentaActual[0].ListaDetalleVenta[i].oPromocionAplicada[p].eTipoBono);
                            oEntDetalleVentaResNST.lstEntPromocionAplicadaNST.Add(oEntPromocionAplicadaNST);
                        }
                        for (int r = 0; r < oRespuestaCotizarVentas.ListaVentaActual[0].ListaDetalleVenta[i].lstEntDetalleRegaloDisp.Length; r++)
                        {
                            EntDetalleRegaloNST oEntDetalleRegaloNST = new EntDetalleRegaloNST();
                            oEntDetalleRegaloNST.descripcion = oRespuestaCotizarVentas.ListaVentaActual[0].ListaDetalleVenta[i].lstEntDetalleRegaloDisp[r].descripcion;
                            oEntDetalleRegaloNST.esSeleccionado = oRespuestaCotizarVentas.ListaVentaActual[0].ListaDetalleVenta[i].lstEntDetalleRegaloDisp[r].esSeleccionado;
                            oEntDetalleRegaloNST.promocionId = oRespuestaCotizarVentas.ListaVentaActual[0].ListaDetalleVenta[i].lstEntDetalleRegaloDisp[r].idPromocion;
                            oEntDetalleRegaloNST.SKU = oRespuestaCotizarVentas.ListaVentaActual[0].ListaDetalleVenta[i].lstEntDetalleRegaloDisp[r].sku;
                            oEntDetalleVentaResNST.lstEntDetalleRegaloNST.Add(oEntDetalleRegaloNST);
                        }

                        oEntRespuestaCotizarVentasCredNST.lstEntDetalleVentaResNST.Add(oEntDetalleVentaResNST);
                    }
                }
                else
                {
                    if (!string.IsNullOrEmpty(oRespuestaCotizarVentas.ListaVentaActual[0].Mensaje))
                    {
                        oEntRespuestaCotizarVentasCredNST.oEntRespuestaNST.eTipoError = (EnumTipoErrorNST)Convert.ToInt32(oRespuestaCotizarVentas.ListaVentaActual[0].Respuesta);
                        oEntRespuestaCotizarVentasCredNST.oEntRespuestaNST.mensajeError = oEntRespuestaNST.mensajeError + oRespuestaCotizarVentas.ListaVentaActual[0].Mensaje;
                    }
                    else
                    {
                        oEntRespuestaCotizarVentasCredNST.oEntRespuestaNST.eTipoError = EnumTipoErrorNST.Warning;
                        oEntRespuestaCotizarVentasCredNST.oEntRespuestaNST.mensajeError = "No hay plazos válidos para la venta";
                    }
                }
                if (!oEntRespuestaCotizarVentasCredNST.EsNuevoEsquemaCredito)
                    oEntRespuestaCotizarVentasCredNST.lstEntInfoPlazoNSTQ = new ManejadorVentaNST().CreaPagosQuincenales(oEntRespuestaCotizarVentasCredNST.lstEntInfoPlazoNST, oEntRespuestaCotizarVentasCredNST.montoFinanciar);
                oEntRespuestaCotizarVentasCredNST.aplicaEntregaDomicilio = oRespuestaCotizarVentas.ListaVentaActual[0].aplicaEntregaDomicilio;
            }
            else
            {
                if (oEntRespuestaNST.eTipoError == EnumTipoErrorNST.SinError)
                    oEntRespuestaCotizarVentasCredNST.oEntRespuestaNST.eTipoError = (EnumTipoErrorNST)Convert.ToInt32(oRespuestaCotizarVentas.Resultado.ExisteError);
                else
                    oEntRespuestaCotizarVentasCredNST.oEntRespuestaNST.eTipoError = oEntRespuestaNST.eTipoError;

                oEntRespuestaCotizarVentasCredNST.oEntRespuestaNST.mensajeError = oEntRespuestaNST.mensajeError + oRespuestaCotizarVentas.Resultado.Mensaje;
            }
            if (this.oEntSeguroIpad.SKU > 0)
            {
                EntDetalleVentaResNST oEntDetalleVentaResNST = new EntDetalleVentaResNST();
                if (oRespuestaCotizarVentas.ListaSeguros != null && oRespuestaCotizarVentas.ListaSeguros.Length > 0)
                {
                    if (oRespuestaCotizarVentas.ListaSeguros[0].lstAtributos.Length > 0)
                        oEntDetalleVentaResNST = this.CrearRespuestaDetalleSeguroVida(oRespuestaCotizarVentas.ListaSeguros[0].lstAtributos);
                }
                else
                {
                    oEntDetalleVentaResNST = this.CrearRespuestaDetalleSeguroVida();
                }
                if (oEntDetalleVentaResNST.SKU > 0)
                {
                    oEntRespuestaCotizarVentasCredNST.lstEntDetalleVentaResNST.Add(oEntDetalleVentaResNST);
                    totServicios += oEntDetalleVentaResNST.precioLista;
                }
            }

            oEntRespuestaCotizarVentasCredNST.totalVentaServicios = totServicios;
            oEntRespuestaCotizarVentasCredNST.aplicaFleteGratis = aplicaFG;
            oEntRespuestaCotizarVentasCredNST.EsNuevoEsquemaCredito = this.SeConsultaNuevoCredito(1532, 3);


            return oEntRespuestaCotizarVentasCredNST;
        }

        private ClienteIpadBase CrearClienteIpadBase(EntClienteNST oEntClienteNST, EntAccionesCreditoNST oEntAccionesCreditoNST, EntComplementosNST oEntComplementosNST)
        {
            ClienteIpadBase oClienteIpadBase = new ClienteIpadBase();
            oClienteIpadBase.CanalCESpecified = oClienteIpadBase.CanalCUSpecified =
                oClienteIpadBase.DigitoVerificadorCESpecified = oClienteIpadBase.EsClienteJCCSpecified =
                oClienteIpadBase.esEngancheInicialSpecified = oClienteIpadBase.FolioCUSpecified =
                oClienteIpadBase.FolioJCCSpecified = oClienteIpadBase.IdClienteCESpecified =
                oClienteIpadBase.NegocioCESpecified = oClienteIpadBase.PaisCUSpecified = oClienteIpadBase.RegionalJCCSpecified =
                oClienteIpadBase.TiendaCESpecified = oClienteIpadBase.TiendaCUSpecified = oClienteIpadBase.esPeticionEPOSSpecified =
                oClienteIpadBase.AplicaClienteAAASpecified = oClienteIpadBase.AplicaClienteBSpecified = oClienteIpadBase.SolicitaClienteBSpecified = oClienteIpadBase.AplicaClienteVSpecified =
                oClienteIpadBase.AplicaPlazoCSpecified = oClienteIpadBase.AplicaPlazoISpecified =
                oClienteIpadBase.MontoMaxFinSpecified = oClienteIpadBase.TipoEsqPagoSpecified = oClienteIpadBase.CteSinPedidosSpecified = oClienteIpadBase.EsquemaTelPresSpecified = oClienteIpadBase.IngresosAComprobarSpecified =
                oClienteIpadBase.Plazo140ITKSpecified = oClienteIpadBase.Plazo142ITKSpecified = oClienteIpadBase.Plazo154ITKSpecified =
                oClienteIpadBase.ProductoRescateSpecified = oClienteIpadBase.PlazoRescateSpecified = oClienteIpadBase.MontoRescateSpecified = oClienteIpadBase.EngancheRescateSpecified = oClienteIpadBase.EsVentaClienteMonederoLealtadSpecified =
                oClienteIpadBase.EstatusClienteSpecified = oClienteIpadBase.PlazoNITKSpecified = oClienteIpadBase.CteTipoRescateSpecified = oClienteIpadBase.MontoMinPlazo128CONSpecified = oClienteIpadBase.PromocionBFCreditoSpecified =
                oClienteIpadBase.prioridadPromoSpecified = oClienteIpadBase.aplicaCuponPromoBFSpecified = oClienteIpadBase.aplicaPromocionesSpecified = true;


            oClienteIpadBase.esPeticionEPOS = true;
            oClienteIpadBase.OrigenDeCotizacion = EnumOrigenDeCotizacion.EPOS;
            oClienteIpadBase.OrigenDeCotizacionSpecified = true;
            if (oEntClienteNST != null)
            {

                oClienteIpadBase.CanalCE = oEntClienteNST.canalCE;
                oClienteIpadBase.CanalCU = Convert.ToByte(oEntClienteNST.canalCU);
                oClienteIpadBase.DigitoVerificadorCE = Convert.ToByte(oEntClienteNST.digitoVerificadorCE);
                oClienteIpadBase.FolioCU = oEntClienteNST.folioCU;
                oClienteIpadBase.IdClienteCE = oEntClienteNST.idClienteCE;
                oClienteIpadBase.NegocioCE = Convert.ToByte(oEntClienteNST.negocioCE);
                oClienteIpadBase.PaisCU = Convert.ToByte(oEntClienteNST.paisCU);
                oClienteIpadBase.TiendaCE = oEntClienteNST.tiendaCE;
                oClienteIpadBase.TiendaCU = oEntClienteNST.tiendaCU;
                oClienteIpadBase.AplicaClienteAAA = oEntClienteNST.aplicaClienteAAA;
                oClienteIpadBase.AplicaClienteB = oEntClienteNST.aplicaClienteB;
                oClienteIpadBase.AplicaClienteV = oEntClienteNST.aplicaClienteV;
                oClienteIpadBase.AplicaPlazoC = oEntClienteNST.aplicaPlazoC;
                oClienteIpadBase.AplicaPlazoI = oEntClienteNST.aplicaPlazoI;
                oClienteIpadBase.MensajeClienteB = oEntClienteNST.mensajeClienteB;
                oClienteIpadBase.MontoMaxFin = oEntClienteNST.montoMaxFin;
                oClienteIpadBase.SolicitaClienteB = oEntClienteNST.solicitaClienteB;
                oClienteIpadBase.TipoEsqPago = oEntClienteNST.tipoEsqPago;
                oClienteIpadBase.CteSinPedidos = oEntClienteNST.CteSinPedidos;
                oClienteIpadBase.EsquemaTelPres = oEntClienteNST.EsquemaTelPres;
                oClienteIpadBase.IngresosAComprobar = oEntClienteNST.IngresosAComprobar;
                oClienteIpadBase.Plazo140ITK = oEntClienteNST.Plazo140ITK;
                oClienteIpadBase.Plazo142ITK = oEntClienteNST.Plazo142ITK;
                oClienteIpadBase.Plazo154ITK = oEntClienteNST.Plazo154ITK;
                oClienteIpadBase.MontoRescate = oEntClienteNST.MontoRescate;
                oClienteIpadBase.PlazoRescate = oEntClienteNST.PlazoRescate;
                oClienteIpadBase.ProductoRescate = oEntClienteNST.ProductoRescate;
                oClienteIpadBase.EngancheRescate = oEntClienteNST.EngancheRescate;
                oClienteIpadBase.EsVentaClienteMonederoLealtad = oEntClienteNST.EsVentaClienteMonederoLealtad;
                oClienteIpadBase.EstatusCliente = oEntClienteNST.EstatusCliente;

                oClienteIpadBase.PlazoNITK = oEntClienteNST.PlazoNITK;
                oClienteIpadBase.PlazosRescate = oEntClienteNST.PlazosRescate;
                oClienteIpadBase.CteTipoRescate = oEntClienteNST.CteTipoRescate;

                oClienteIpadBase.MontoMinPlazo128CON = oEntClienteNST.MontoMinPlazo128CON;
                oClienteIpadBase.PlazosEspecialesCon = oEntClienteNST.PlazosEspecialesCon;
                oClienteIpadBase.PlazosEspecialesMovilidad = oEntClienteNST.PlazosEspecialesMovilidad;

                if (oEntClienteNST.folioCU > 0 && oEntClienteNST.TasaCON != null && oEntClienteNST.TasaITK != null && oEntClienteNST.TasaTEL != null)
                {
                    oClienteIpadBase.TasaCON = oEntClienteNST.TasaCON;
                    oClienteIpadBase.TasaITK = oEntClienteNST.TasaITK;
                    oClienteIpadBase.TasaTEL = oEntClienteNST.TasaTEL;
                }
                else
                {
                    oClienteIpadBase.TasaCON = 3;
                    oClienteIpadBase.TasaITK = 3;
                    oClienteIpadBase.TasaTEL = 3;
                }
                oClienteIpadBase.TasaCONSpecified = true;
                oClienteIpadBase.TasaITKSpecified = true;
                oClienteIpadBase.TasaTELSpecified = true;

                oClienteIpadBase.aplicaCuponPromoBF = oEntClienteNST.aplicaCuponBF;
                oClienteIpadBase.cuponPromoBF = oEntClienteNST.cuponPromoBF;
                oClienteIpadBase.PromocionBFCredito = oEntClienteNST.aplicaPromocionBF;
                oClienteIpadBase.prioridadPromo = oEntClienteNST.prioridadPromo;
                oClienteIpadBase.aplicaPromociones = oEntClienteNST.aplicaPromociones;
                oClienteIpadBase.DatosRecomendador = new EntPromocionPDVTelOUI();
                oClienteIpadBase.DatosRecomendador.clienteUnico = oEntClienteNST.DatosRecomendador.clienteUnico;
                oClienteIpadBase.DatosRecomendador.codigoCanje = oEntClienteNST.DatosRecomendador.codigoCanje;
                oClienteIpadBase.DatosRecomendador.montoVenta = oEntClienteNST.DatosRecomendador.montoVenta;
                oClienteIpadBase.DatosRecomendador.montoVentaSpecified = true;
                oClienteIpadBase.DatosRecomendador.portalVenta = oEntClienteNST.DatosRecomendador.portalVenta;
                oClienteIpadBase.DatosRecomendador.tipoCliente = oEntClienteNST.DatosRecomendador.tipoCliente;
                oClienteIpadBase.DatosRecomendador.tipoClienteSpecified = true;

                if (oEntClienteNST.ResultadoApi != null)
                {
                    oClienteIpadBase.ConsultaApi = oEntClienteNST.ConsultaApi;
                    oClienteIpadBase.ConsultaApiSpecified = true;

                    oClienteIpadBase.ResultadoApi = new EntResultadoAPIIPAD();
                    int totalProductos = oEntClienteNST.ResultadoApi.ProductosTipoCliente.Length;
                    oClienteIpadBase.ResultadoApi.ProductosTipoCliente = new EntProductosTipoClienteApiCteIPAD[totalProductos];

                    for (int indicePro = 0; indicePro < totalProductos; indicePro++)
                    {
                        oClienteIpadBase.ResultadoApi.ProductosTipoCliente[indicePro] = new EntProductosTipoClienteApiCteIPAD();
                        oClienteIpadBase.ResultadoApi.ProductosTipoCliente[indicePro].IdProducto = oEntClienteNST.ResultadoApi.ProductosTipoCliente[indicePro].IdProducto;
                        oClienteIpadBase.ResultadoApi.ProductosTipoCliente[indicePro].CodigoPromocionTasa = oEntClienteNST.ResultadoApi.ProductosTipoCliente[indicePro].CodigoPromocionTasa;
                        oClienteIpadBase.ResultadoApi.ProductosTipoCliente[indicePro].CodigoTipoTasa = oEntClienteNST.ResultadoApi.ProductosTipoCliente[indicePro].CodigoTipoTasa;
                        oClienteIpadBase.ResultadoApi.ProductosTipoCliente[indicePro].CeroEnganche = oEntClienteNST.ResultadoApi.ProductosTipoCliente[indicePro].CeroEnganche;
                        oClienteIpadBase.ResultadoApi.ProductosTipoCliente[indicePro].Renovable = oEntClienteNST.ResultadoApi.ProductosTipoCliente[indicePro].Renovable;
                        oClienteIpadBase.ResultadoApi.ProductosTipoCliente[indicePro].Restituible = oEntClienteNST.ResultadoApi.ProductosTipoCliente[indicePro].Restituible;

                        oClienteIpadBase.ResultadoApi.ProductosTipoCliente[indicePro].IdProductoSpecified =
                            oClienteIpadBase.ResultadoApi.ProductosTipoCliente[indicePro].CeroEngancheSpecified =
                            oClienteIpadBase.ResultadoApi.ProductosTipoCliente[indicePro].RenovableSpecified =
                            oClienteIpadBase.ResultadoApi.ProductosTipoCliente[indicePro].RestituibleSpecified = true;
                    }
                }

                if (oEntClienteNST.LstPedidosRenovacion != null && oEntClienteNST.LstPedidosRenovacion.Count > 0)
                {
                    int i = 0;
                    oClienteIpadBase.LstPedidosRenovacion = new EntPedidoRenovacion[oEntClienteNST.LstPedidosRenovacion.Count];
                    foreach (EntPedidoRenovacionNST PedidoRen in oEntClienteNST.LstPedidosRenovacion)
                    {
                        EntPedidoRenovacion _Pedido = new EntPedidoRenovacion();
                        _Pedido.CanalCU = PedidoRen.CanalCU;
                        _Pedido.CanalCUSpecified = true;
                        _Pedido.SucursalCU = PedidoRen.SucursalCU;
                        _Pedido.SucursalCUSpecified = true;
                        _Pedido.FolioCU = PedidoRen.FolioCU;
                        _Pedido.FolioCUSpecified = true;
                        _Pedido.NegocioCte = PedidoRen.NegocioCte;
                        _Pedido.NegocioCteSpecified = true;
                        _Pedido.TiendaCte = PedidoRen.TiendaCte;
                        _Pedido.TiendaCteSpecified = true;
                        _Pedido.CteId = PedidoRen.CteId;
                        _Pedido.CteIdSpecified = true;
                        _Pedido.DigitoVer = PedidoRen.DigitoVer;
                        _Pedido.DigitoVerSpecified = true;
                        _Pedido.Nombre = PedidoRen.Nombre;
                        _Pedido.APaterno = PedidoRen.APaterno;
                        _Pedido.AMaterno = PedidoRen.AMaterno;
                        _Pedido.CalleCte = PedidoRen.CalleCte;
                        _Pedido.NoExt = PedidoRen.NoExt;
                        _Pedido.NoInt = PedidoRen.NoInt;
                        _Pedido.CPCte = PedidoRen.CPCte;
                        _Pedido.CanalPed = PedidoRen.CanalPed;
                        _Pedido.CanalPedSpecified = true;
                        _Pedido.SucursalPed = PedidoRen.SucursalPed;
                        _Pedido.SucursalPedSpecified = true;
                        _Pedido.Pedido = PedidoRen.Pedido;
                        _Pedido.PedidoSpecified = true;
                        _Pedido.AbonoNormal = PedidoRen.AbonoNormal;
                        _Pedido.AbonoNormalSpecified = true;
                        _Pedido.AbonoPuntual = PedidoRen.AbonoPuntual;
                        _Pedido.AbonoPuntualSpecified = true;
                        _Pedido.SaldoCapital = PedidoRen.SaldoCapital;
                        _Pedido.SaldoCapitalSpecified = true;
                        _Pedido.Plazo = PedidoRen.Plazo;
                        _Pedido.PlazoSpecified = true;
                        _Pedido.Fitir = PedidoRen.Fitir;
                        _Pedido.FitirSpecified = true;
                        _Pedido.PeriodoCor = PedidoRen.PeriodoCor;
                        _Pedido.PeriodoCorSpecified = true;
                        _Pedido.PeriodoAct = PedidoRen.PeriodoAct;
                        _Pedido.PeriodoActSpecified = true;
                        _Pedido.Pagando = PedidoRen.Pagando;
                        _Pedido.PagandoSpecified = true;
                        _Pedido.Bonifica = PedidoRen.Bonifica;
                        _Pedido.BonificaSpecified = true;
                        _Pedido.CapacidadALiberar = PedidoRen.CapacidadALiberar;
                        _Pedido.CapacidadALiberarSpecified = true;
                        _Pedido.ClaveRenovacion = PedidoRen.ClaveRenovacion;
                        _Pedido.ClaveRenovacionSpecified = true;

                        oClienteIpadBase.LstPedidosRenovacion[i] = new EntPedidoRenovacion();
                        oClienteIpadBase.LstPedidosRenovacion[i] = _Pedido;
                        i++;
                    }
                }

                if (oClienteIpadBase.FolioCU > 0)
                    this.ContieneClienteContado = true;

                if (oEntAccionesCreditoNST != null)
                    oClienteIpadBase.esEngancheInicial = oEntAccionesCreditoNST.esEngancheInicial;
            }

            if (oEntComplementosNST != null && oEntComplementosNST.oEntRecompensasRecomendar != null)
            {
                oClienteIpadBase.RecompensasRecomendar = new EntRecompensasRecomendar
                {
                    ClienteUnico = oEntComplementosNST.oEntRecompensasRecomendar.ClienteUnico,
                    DescuentoOtorgado = oEntComplementosNST.oEntRecompensasRecomendar.DescuentoOtorgado,
                    EsAplicar = oEntComplementosNST.oEntRecompensasRecomendar.EsAplicar,
                    EsAplicarSpecified = true,
                    EsConsultarBanco = oEntComplementosNST.oEntRecompensasRecomendar.EsConsultarBanco,
                    EsConsultarBancoSpecified = true,
                    MontoCompra = oEntComplementosNST.oEntRecompensasRecomendar.MontoCompra,
                    RespuestaCaracteristica = new EntRespuestaCaracteristica()
                    {
                        AplicaRango = oEntComplementosNST.oEntRecompensasRecomendar.RespuestaCaracteristica.data.aplicaRango,
                        AplicaRangoSpecified = true,
                        DescuentoCada = oEntComplementosNST.oEntRecompensasRecomendar.RespuestaCaracteristica.data.descuentoCada,
                        Mensaje = oEntComplementosNST.oEntRecompensasRecomendar.RespuestaCaracteristica.data.mensaje,
                        MontoMinimoCompra = oEntComplementosNST.oEntRecompensasRecomendar.RespuestaCaracteristica.data.montoMinimoCompra,
                        MontoPremio = oEntComplementosNST.oEntRecompensasRecomendar.RespuestaCaracteristica.data.montoPremio,
                        TienePromocion = oEntComplementosNST.oEntRecompensasRecomendar.RespuestaCaracteristica.data.tienePromocion,
                        TienePromocionSpecified = true
                    }
                };
            }

            return oClienteIpadBase;
        }

        private void AplicarSurtimiento(EntRespuestaVentaNST oEntRespuestaVentaNST, List<EntDetalleVentaBaseNST> lstEntDetalleVentaBaseNST, string idUsuario, string ws)
        {
            try
            {
                ProductosPedido oProductosPedido = new ProductosPedido();
                Resultado oResultado = new Resultado();
                WSSurtimientoContado oWSSurtimientoContado = new WSSurtimientoContado();
                oProductosPedido = oWSSurtimientoContado.ConsultaPedido(oEntRespuestaVentaNST.idPedido.ToString(), idUsuario, ws);
                this.AsociarSeries(lstEntDetalleVentaBaseNST, oProductosPedido.DetallesProducto);
                oResultado = oWSSurtimientoContado.AplicarSurtimientoContado(oProductosPedido, idUsuario, ws, "");

                if (oResultado.Respuesta > 0)
                {
                    oEntRespuestaVentaNST.surtCorrecto = true;
                    RespuestaIpad oRespuestaIpad = new CtrlImpresionIpad().GeneraImpresionSurtimientoIpad(oEntRespuestaVentaNST.idPedido, oEntRespuestaVentaNST.idUniticket.ToString(), idUsuario, ws);
                    if (!oRespuestaIpad.Exito)
                    {
                        oEntRespuestaVentaNST.oEntRespuestaNST.eTipoError = EnumTipoErrorNST.Warning;
                        oEntRespuestaVentaNST.oEntRespuestaNST.mensajeError = oRespuestaIpad.MensajeTecnico;
                    }
                }
                else
                {
                    oEntRespuestaVentaNST.oEntRespuestaNST.eTipoError = EnumTipoErrorNST.Warning;
                    oEntRespuestaVentaNST.oEntRespuestaNST.mensajeError = oResultado.MensajeUsuario;
                }
            }
            catch (Exception ex)
            {
                oEntRespuestaVentaNST.oEntRespuestaNST.eTipoError = EnumTipoErrorNST.Error;
                oEntRespuestaVentaNST.oEntRespuestaNST.mensajeError = "Error: Durante el surtimiento - " + ex.Message;
            }
        }

        private void AsociarSeries(List<EntDetalleVentaBaseNST> lstEntDetalleVentaBaseNST, DetalleProducto[] lstDetalleProducto)
        {
            for (int i = 0; i < lstDetalleProducto.Length; i++)
            {
                for (int j = 0; j < lstEntDetalleVentaBaseNST.Count; j++)
                {
                    if (lstDetalleProducto[i].Sku == lstEntDetalleVentaBaseNST[j].SKU)
                    {
                        lstDetalleProducto[i].Serie = new string[lstEntDetalleVentaBaseNST.Count];
                        for (int x = 0; x < lstEntDetalleVentaBaseNST[j].lstSeries.Count; x++)
                            lstDetalleProducto[i].Serie[x] = lstEntDetalleVentaBaseNST[j].lstSeries[x].serie;
                    }
                }
            }
        }

        private InformacionEmpleado CrearInformacionEmpleado(EntVentaEmpleadoNST oEntVentaEmpleadoNST)
        {
            InformacionEmpleado oInformacionEmpleado = null;
            if (oEntVentaEmpleadoNST != null && !string.IsNullOrEmpty(oEntVentaEmpleadoNST.DescCompania) && oEntVentaEmpleadoNST.NoEmpleado > 0)
            {
                oInformacionEmpleado = new InformacionEmpleado();
                oInformacionEmpleado.DescCompania = oEntVentaEmpleadoNST.DescCompania;
                oInformacionEmpleado.NoEmpleado = oEntVentaEmpleadoNST.NoEmpleado;
                oInformacionEmpleado.NoEmpleadoSpecified = true;
            }
            return oInformacionEmpleado;
        }

        private void ValidarVentaApartado(EntVentaActualCotizar[] lstEntVentaActualCotizar, List<EntPromocionEspecialNST> lstEntPromocionEspecialNST)
        {
            if (lstEntPromocionEspecialNST != null && lstEntPromocionEspecialNST.Count > 0)
            {
                for (int i = 0; i < lstEntPromocionEspecialNST.Count; i++)
                {
                    if (lstEntPromocionEspecialNST[i].eTipoPromocion == EnumTipoPromocionNST.VentaApartado)
                    {
                        for (int j = 0; j < lstEntVentaActualCotizar.Count(); j++)
                        {
                            lstEntVentaActualCotizar[j].TipoVenta = EnumTipoVenta.apartado;
                            lstEntVentaActualCotizar[j].MontoEngancheVenta = lstEntPromocionEspecialNST[i].oEntDatosVentaApartado.montoApartado;
                        }
                    }
                }
            }
        }

        private void GenerarVentaApartado(RespuestaCotizarVentas oRespuestaCotizarVentas, List<EntPromocionEspecialNST> lstEntPromocionEspecialNST)
        {
            if (lstEntPromocionEspecialNST != null && lstEntPromocionEspecialNST.Count > 0)
            {
                for (int i = 0; i < lstEntPromocionEspecialNST.Count; i++)
                {
                    if (lstEntPromocionEspecialNST[i].eTipoPromocion == EnumTipoPromocionNST.VentaApartado)
                    {
                        lstEntPromocionEspecialNST[i].oEntDatosVentaApartado.fechaApartado = DateTime.Now.AddMonths(3).ToShortDateString();
                        lstEntPromocionEspecialNST[i].oEntDatosVentaApartado.montoApartado = oRespuestaCotizarVentas.ListaVentaActual[0].MontoEnganche;
                    }
                }
            }
        }

        public EntRespuestaProductosPaquete ObtieneProductosEnPaquete(string CadenaProd, int tipoVenta)
        {
            EntRespuestaProductosPaquete response = new EntRespuestaProductosPaquete();

            DataSet dSet = new DataSet();

            try
            {
                EntConsultasBDNST consBD = new EntConsultasBDNST();
                dSet = consBD.ConsultaPromocionPaquete(CadenaProd, tipoVenta);

                if (dSet != null && dSet.Tables != null && dSet.Tables.Count > 0 && dSet.Tables[0].Rows.Count > 0)
                {
                    foreach (DataRow EntProductoPaquete in dSet.Tables[0].Rows)
                    {
                        EntProductoMSITAZ prod = new EntProductoMSITAZ();

                        if (!(EntProductoPaquete["fiProdId"] is DBNull))
                            prod.ProductoId = Convert.ToInt32(EntProductoPaquete["fiProdId"]);
                        if (!(EntProductoPaquete["fcProdDesc"] is DBNull))
                            prod.Descripcion = EntProductoPaquete["fcProdDesc"].ToString().Trim();
                        if (!(EntProductoPaquete["fnprodprecio"] is DBNull))
                            prod.Precio = Convert.ToDecimal(EntProductoPaquete["fnprodprecio"]);

                        response.ProductosEnPaquete.Add(prod);
                    }
                }

            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine("Error al consultar promociones de paquete. Error: " + ex.Message + " stacktrace:" + ex.StackTrace, "LOG");
                response = new EntRespuestaProductosPaquete();
            }

            return response;
        }

        private void ConsultarDescuentosCredito(List<EntDetalleVentaBaseNST> lstEntDetalleVentaBaseNST, List<EntDetalleVentaResNST> lstEntDetalleVentaResNST, List<EntPromocionEspecialNST> lstEntPromocionEspecialNST)
        {
            try
            {
                string cadenaSkus = new ManejadorRutinasNST().GeneraCadenaSkus(lstEntDetalleVentaBaseNST, false, false);
                int idPromocion = 0;
                if (lstEntPromocionEspecialNST != null && lstEntPromocionEspecialNST.Count > 0)
                    for (int i = 0; i < lstEntPromocionEspecialNST.Count; i++)
                        idPromocion = lstEntPromocionEspecialNST[0].promocionId;

                new ManejadorPromocionesNST().ConsultarDescuentosCredito(lstEntDetalleVentaResNST, cadenaSkus, idPromocion);
            }
            catch { }
        }

        private DatosFacturacion CrearDatosFacturacion(bool esDesgloceIva, EntDatosEntradaNST oEntDatosEntradaNST, double idPedido)
        {
            DatosFacturacion oDatosFacturacion = new DatosFacturacion();
            oDatosFacturacion.DesglosaIva = esDesgloceIva;
            oDatosFacturacion.Empleado = oEntDatosEntradaNST.idUsuario;
            oDatosFacturacion.EstacionTrabajo = oEntDatosEntradaNST.ws;
            oDatosFacturacion.Pedido = idPedido;
            oDatosFacturacion.IdSession = oEntDatosEntradaNST.idSesion;

            oDatosFacturacion.Negocio = 1;
            oDatosFacturacion.TFactura = EnumTiposDeFactura.Factura;
            oDatosFacturacion.TOperacion = EnumTOperacion.ingreso;
            oDatosFacturacion.TSolictud = EnumSolicitudFactura.FacturaBajoSolicitudCliente;
            oDatosFacturacion.Transaccion = 2318;

            oDatosFacturacion.DesglosaIvaSpecified = oDatosFacturacion.NegocioSpecified = oDatosFacturacion.PedidoSpecified =
            oDatosFacturacion.TFacturaSpecified = oDatosFacturacion.TOperacionSpecified = oDatosFacturacion.TransaccionSpecified =
            oDatosFacturacion.TSolictudSpecified = true;

            return oDatosFacturacion;
        }

        private DatosCliente CrearDatosClienteFacturacion(EntClienteFacturaNST oEntClienteFacturaNST)
        {
            DatosCliente oDatosCliente = new DatosCliente();
            oDatosCliente.Calle = oEntClienteFacturaNST.calle;
            oDatosCliente.CodigoPostal = oEntClienteFacturaNST.cp;
            oDatosCliente.Colonia = oEntClienteFacturaNST.colonia;
            oDatosCliente.NoExterior = oEntClienteFacturaNST.numeroExt;
            oDatosCliente.NoInterior = oEntClienteFacturaNST.numeroInt;
            oDatosCliente.Nombre = oEntClienteFacturaNST.nombreCompleto;
            oDatosCliente.Rfc = oEntClienteFacturaNST.RFCCliente;
            return oDatosCliente;
        }

        private List<string> GenerarRespuestaFacturacion(ContenedorFacturas oContenedorFacturas)
        {
            List<string> lstFacturas = new List<string>();
            if (oContenedorFacturas.Facturas.Length > 0)
            {
                for (int i = 0; i < oContenedorFacturas.Facturas.Length; i++)
                {
                    lstFacturas.Add(oContenedorFacturas.Facturas[i].Xml);
                }
            }
            return lstFacturas;
        }

        private EntPresupuestoResNST GeneraEntPresupuestoResNST(EntPresupuestoRes oEntPresupuestoRes)
        {
            EntPresupuestoResNST oEntPresupuestoResNST = new EntPresupuestoResNST();
            if (oEntPresupuestoRes.oEntRespuesta.ExisteError)
            {
                oEntPresupuestoResNST.oEntRespuestaNST.eTipoError = EnumTipoErrorNST.Warning;
                oEntPresupuestoResNST.oEntRespuestaNST.mensajeError = oEntPresupuestoRes.oEntRespuesta.Mensaje;
            }
            else
            {
                oEntPresupuestoResNST.abonoNST = oEntPresupuestoRes.abono;
                switch (oEntPresupuestoRes.ePeriodos)
                {
                    case EnumPeriodos.Semanal:
                        oEntPresupuestoResNST.ePeriodoNST = EnumPeriodoNST.Semanal;
                        break;
                    case EnumPeriodos.Mensual:
                        oEntPresupuestoResNST.ePeriodoNST = EnumPeriodoNST.Mensual;
                        break;
                    case EnumPeriodos.Quincenal:
                        oEntPresupuestoResNST.ePeriodoNST = EnumPeriodoNST.Quincenal;
                        break;
                }
                switch (oEntPresupuestoRes.eTipoVenta)
                {
                    case EnumTipoVenta.apartado:
                        oEntPresupuestoResNST.eTipoVentaNST = EnumTipoVentaNST.Apartado;
                        break;
                    case EnumTipoVenta.contado:
                        oEntPresupuestoResNST.eTipoVentaNST = EnumTipoVentaNST.Contado;
                        break;
                    case EnumTipoVenta.credito:
                        oEntPresupuestoResNST.eTipoVentaNST = EnumTipoVentaNST.Credito;
                        break;
                    case EnumTipoVenta.mostrador:
                        oEntPresupuestoResNST.eTipoVentaNST = EnumTipoVentaNST.Mostrador;
                        break;
                }
                for (int i = 0; i < oEntPresupuestoRes.lstDetalleVentaRes.Length; i++)
                {
                    EntDetalleProductoConsultaNST oEntDetalleProductoConsultaNST = new EntDetalleProductoConsultaNST();
                    oEntDetalleProductoConsultaNST.SKU = oEntPresupuestoRes.lstDetalleVentaRes[i].SKU;
                    oEntDetalleProductoConsultaNST.descripcion = oEntPresupuestoRes.lstDetalleVentaRes[i].Descripcion;
                    oEntDetalleProductoConsultaNST.precioLista = oEntPresupuestoRes.lstDetalleVentaRes[i].PrecioLista;
                    oEntDetalleProductoConsultaNST.Cantidad = oEntPresupuestoRes.lstDetalleVentaRes[i].Cantidad;
                    oEntDetalleProductoConsultaNST.existencia = oEntPresupuestoRes.lstDetalleVentaRes[i].Existencia;
                    oEntPresupuestoResNST.lstEntDetalleProductoConsultaNST.Add(oEntDetalleProductoConsultaNST);
                }
                oEntPresupuestoResNST.pagoPuntualNST = oEntPresupuestoRes.pagoPuntual;
                oEntPresupuestoResNST.totalPagarNST = oEntPresupuestoRes.totalPagar;
                oEntPresupuestoResNST.totalVentaNST = oEntPresupuestoRes.totalVenta;
                oEntPresupuestoResNST.ultimoAbonoNST = oEntPresupuestoRes.ultimoAbono;
                oEntPresupuestoResNST.vigenciaNST = oEntPresupuestoRes.vigencia;
                oEntPresupuestoResNST.plazoSeleccionado = oEntPresupuestoRes.plazoSeleccionado;
            }
            return oEntPresupuestoResNST;
        }

        private EntRespuestaConProductoNST GeneraEntDetalleVentaResNST(DetalleVentaRes[] lstDetalleVentaRes)
        {
            EntRespuestaConProductoNST oEntRespuestaConProductoNST = new EntRespuestaConProductoNST();
            if (lstDetalleVentaRes.Length > 0)
            {
                for (int i = 0; i < lstDetalleVentaRes.Length; i++)
                {
                    oEntRespuestaConProductoNST.oEntDetalleProductoConsultaNST.SKU = lstDetalleVentaRes[i].SKU;
                    oEntRespuestaConProductoNST.oEntDetalleProductoConsultaNST.precioLista = lstDetalleVentaRes[i].PrecioLista;
                    oEntRespuestaConProductoNST.oEntDetalleProductoConsultaNST.descripcion = lstDetalleVentaRes[i].Descripcion;
                    oEntRespuestaConProductoNST.oEntDetalleProductoConsultaNST.existencia = lstDetalleVentaRes[i].Existencia;
                    oEntRespuestaConProductoNST.oEntDetalleProductoConsultaNST.eTipoProductoNST = this.ValidarTipoProducto(lstDetalleVentaRes[i].TipoProducto);
                }
            }
            else
            {
                oEntRespuestaConProductoNST.oEntRespuestaNST.eTipoError = EnumTipoErrorNST.Warning;
                oEntRespuestaConProductoNST.oEntRespuestaNST.mensajeError = "No existe información del producto";
            }
            return oEntRespuestaConProductoNST;
        }

        private EnumTipoProductoNST ValidarTipoProducto(EnumTipoProductos eTipoProductos)
        {
            EnumTipoProductoNST eTipoProductoNST = EnumTipoProductoNST.mercancias;
            switch (eTipoProductos)
            {
                case EnumTipoProductos.Comercio:
                case EnumTipoProductos.Seguros:
                case EnumTipoProductos.Milenia:
                case EnumTipoProductos.Servicio:
                    eTipoProductoNST = EnumTipoProductoNST.mercancias;
                    break;
                case EnumTipoProductos.Telefonia:
                    eTipoProductoNST = EnumTipoProductoNST.telefonia;
                    break;
                case EnumTipoProductos.Motos:
                    eTipoProductoNST = EnumTipoProductoNST.motos;
                    break;
            }
            return eTipoProductoNST;
        }

        private EntRespuestaEstatusPedidoNST GeneraEntRespuestaEstatusPedidoNST(EntRespuestaEstatusPedido oEntRespuestaEstatusPedido)
        {
            EntRespuestaEstatusPedidoNST oEntRespuestaEstatusPedidoNST = new EntRespuestaEstatusPedidoNST();
            oEntRespuestaEstatusPedidoNST.eTipoStatusPedidoNST = (EnumTipoStatusPedidoNST)Convert.ToInt32(oEntRespuestaEstatusPedido.eTipoStatusPedido);
            if (oEntRespuestaEstatusPedido.oEntRespuesta.ExisteError)
            {
                oEntRespuestaEstatusPedidoNST.oEntRespuestaNST.eTipoError = EnumTipoErrorNST.Warning;
                oEntRespuestaEstatusPedidoNST.oEntRespuestaNST.mensajeError = oEntRespuestaEstatusPedido.oEntRespuesta.Mensaje;
            }
            return oEntRespuestaEstatusPedidoNST;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="oEntRespuestaCotizarVentasCredNST"></param>
        /// <param name="oRespuestaCotizarVentas"></param>
        private void GuardaDescuentoAbonos(EntRespuestaCotizarVentasCredNST oEntRespuestaCotizarVentasCredNST, RespuestaCotizarVentas oRespuestaCotizarVentas)
        {
            var metodo = this.GetType().Name + "." + System.Reflection.MethodBase.GetCurrentMethod().Name;
            _logs.AppendLine(metodo, "Inicia");
            _logs.AppendLineJson(metodo, "parametros:", new { oEntRespuestaCotizarVentasCredNST = oEntRespuestaCotizarVentasCredNST, oRespuestaCotizarVentas = oRespuestaCotizarVentas });
            if (oEntRespuestaCotizarVentasCredNST.lstEntDetalleVentaResNST != null && oEntRespuestaCotizarVentasCredNST.lstEntDetalleVentaResNST.Count > 0)
            {
                foreach (var oEntDetalleVentaResNST in oEntRespuestaCotizarVentasCredNST.lstEntDetalleVentaResNST)
                {
                    foreach (var deta in oRespuestaCotizarVentas.ListaVentaActual[0].ListaDetalleVenta)
                    {
                        if (deta.SKU == oEntDetalleVentaResNST.SKU)
                        {
                            var listaPlazosTmp = deta.listaPlazos.ToList();
                            if (listaPlazosTmp != null && listaPlazosTmp.Count > 0 && oEntRespuestaCotizarVentasCredNST.lstEntInfoPlazoNST.Exists(x => (x.esSeleccionado) && (listaPlazosTmp.Exists(pl => x.plazo == pl.plazo))))
                            {
                                _logs.AppendLine(metodo, "plazo seleccionado");
                                var plazoSeleccionado = oEntRespuestaCotizarVentasCredNST.lstEntInfoPlazoNST.Find(x => (x.esSeleccionado) && (listaPlazosTmp.Exists(pl => x.plazo == pl.plazo)));
                                _logs.AppendLineJson(metodo, "plazoSeleccionado:", plazoSeleccionado);
                                var descuentosAbono = listaPlazosTmp.Find(x => x.plazo == plazoSeleccionado.plazo);
                                _logs.AppendLineJson(metodo, "descuentosAbono:", descuentosAbono);
                                var cantidad = deta.Cantidad;
                                if (descuentosAbono.porcentajeDescuentoSpecified && descuentosAbono.porcentajeDescuento > 0)
                                {
                                    //Calcula abono sin descuento 
                                    double abonoSinDescuento = 0;
                                    if (descuentosAbono.sobrePrecioPuntualOriginal > 0)
                                    {
                                        abonoSinDescuento = Math.Round((descuentosAbono.sobrePrecioPuntualOriginal + descuentosAbono.precioCredito) / descuentosAbono.numeroPagos) * cantidad;
                                        _logs.AppendLineJson(metodo, "sobrePrecioPuntualOriginal", descuentosAbono.sobrePrecioPuntualOriginal);
                                        _logs.AppendLineJson(metodo, "precioCredito", descuentosAbono.precioCredito);
                                        _logs.AppendLineJson(metodo, "numeroPagos", descuentosAbono.numeroPagos);
                                    }
                                    else
                                    {
                                        abonoSinDescuento = Convert.ToDouble(descuentosAbono.descuentoPuntual);
                                    }

                                    //Termina calculo de abono sin                                     
                                    oEntDetalleVentaResNST.descuento = Convert.ToDecimal(descuentosAbono.descuento);
                                    oEntDetalleVentaResNST.descuentoPuntual = Convert.ToDecimal(descuentosAbono.descuentoPuntual);
                                    oEntDetalleVentaResNST.nuevaTasa = Convert.ToDecimal(descuentosAbono.nuevaTasa);
                                    oEntDetalleVentaResNST.nuevaTasaPuntual = Convert.ToDecimal(descuentosAbono.nuevaTasaPuntual);
                                    oEntDetalleVentaResNST.plazo = descuentosAbono.plazo;
                                    oEntDetalleVentaResNST.porcentajeDescuento = Convert.ToDecimal(descuentosAbono.porcentajeDescuento);
                                    oEntDetalleVentaResNST.precioCredito = Convert.ToDecimal(descuentosAbono.precioCredito);
                                    oEntDetalleVentaResNST.sku = descuentosAbono.sku;
                                    oEntDetalleVentaResNST.sobrePrecioOriginal = Convert.ToDecimal(descuentosAbono.sobrePrecioOriginal);
                                    oEntDetalleVentaResNST.sobrePrecioPuntualOriginal = Convert.ToDecimal(descuentosAbono.sobrePrecioPuntualOriginal);
                                    oEntDetalleVentaResNST.tasaOriginal = Convert.ToDecimal(descuentosAbono.tasaOriginal);
                                    oEntDetalleVentaResNST.tasaPuntualOriginal = Convert.ToDecimal(descuentosAbono.tasaPuntualOriginal);
                                    oEntRespuestaCotizarVentasCredNST.aplicaDescuentoAbono = true;
                                    oEntDetalleVentaResNST.AbonoSinDescuento = Convert.ToDecimal(abonoSinDescuento);
                                    oEntRespuestaCotizarVentasCredNST.leyendaEtiqueta = "Por ser Cliente especial recibiste  " + (Convert.ToInt16(oEntDetalleVentaResNST.porcentajeDescuento * 100)) + " % de descuento en el Abono";

                                    _logs.AppendLineJson(metodo, "oEntDetalleVentaResNST:  ", oEntDetalleVentaResNST);
                                }
                            }
                        }
                    }
                }
            }
            _logs.AppendLine(metodo, "Termina");
            _logs.EscribeLog();
        }

        private DataSet GetConfigRecargainicial(int sku)
        {
            Elektra.Negocio.Entidades.PromocionesIPAD.EntPromocionesTelefonia recargaInicial = new Entidades.PromocionesIPAD.EntPromocionesTelefonia();
            DataSet configRecargaInicial = new DataSet();
            configRecargaInicial = recargaInicial.ValidaRecargaInicial(sku);

            if (configRecargaInicial != null && configRecargaInicial.Tables[0] != null && configRecargaInicial.Tables.Count > 0 && configRecargaInicial.Tables[0].Rows.Count > 0)
            {
                return configRecargaInicial;
            }
            else
            {
                return null;
            }
        }

		private DataSet GetConfigRecargainicialLibre(int sku)
        {
            Elektra.Negocio.Entidades.PromocionesIPAD.EntPromocionesTelefonia recargaInicial = new Entidades.PromocionesIPAD.EntPromocionesTelefonia();
            DataSet configRecargaInicialLibre = new DataSet();
            configRecargaInicialLibre = recargaInicial.ValidaRecargaInicialLibre(sku);

            if (configRecargaInicialLibre != null && configRecargaInicialLibre.Tables[0] != null && configRecargaInicialLibre.Tables.Count > 0 && configRecargaInicialLibre.Tables[0].Rows.Count > 0)
            {
                return configRecargaInicialLibre;
            }
            else
            {
                return null;
            }
        }
        private DataSet GetMostrarCreditoGratis()
        {
            Elektra.Negocio.Entidades.PromocionesIPAD.EntPromocionesTelefonia recargaInicial = new Entidades.PromocionesIPAD.EntPromocionesTelefonia();
            DataSet mostrarPopup = new DataSet();
            mostrarPopup = recargaInicial.ValidaPopupCreditoGratis();

            if (mostrarPopup != null && mostrarPopup.Tables[0] != null && mostrarPopup.Tables.Count > 0 && mostrarPopup.Tables[0].Rows.Count > 0)
            {
                return mostrarPopup;
            }
            else
            {
                return null;
            }
        }

        private DataSet GetMostrarModalCombosPUC(int sku)
        {
            Elektra.Negocio.Entidades.PromocionesIPAD.EntPromocionesTelefonia recargaInicial = new Entidades.PromocionesIPAD.EntPromocionesTelefonia();
            DataSet mostrarPopup = new DataSet();
            mostrarPopup = recargaInicial.ValidaModalCombosPUC(sku);

            if (mostrarPopup != null && mostrarPopup.Tables[0] != null && mostrarPopup.Tables.Count > 0 && mostrarPopup.Tables[0].Rows.Count > 0)
            {
                return mostrarPopup;
            }
            else
            {
                return null;
            }
        }

        #endregion

        #region Catalogo Extendido

        /// <summary>
        /// Método que genera registro en tienda de 
        /// </summary>
        /// <param name="skuCatalogo"></param>
        /// <returns></returns>
        public EntRespuestaNST GenerarProductoCatalogoExtendido(int skuCatalogo, double precioSku)
        {
            EntRespuestaNST oEntRespuesta = new EntRespuestaNST();
            try
            {
                Elektra.Negocio.Entidades.Producto.EntProducto oEntProducto = new Elektra.Negocio.Entidades.Producto.EntProducto();
                oEntProducto.BeginObject(skuCatalogo);

                if (oEntProducto.Sku != 0d)
                {
                    oEntRespuesta.eTipoError = EnumTipoErrorNST.SinError;
                    oEntRespuesta.mensajeError = "Producto existe en tienda: " + skuCatalogo + ", TipoProd:" + oEntProducto.Tipo;
                    if (oEntProducto.PrecioLista != precioSku)
                    {
                        //Se actualiza al precio nuevo asignado por Ekt.com
                        oEntRespuesta.mensajeError = "Se actualizará el precio: " + oEntProducto.PrecioLista + " al precio:" + precioSku;

                    }
                }
            }
            catch (Exception ex)
            {
                try
                {
                    string URL = string.Empty;
                    string peticionWS = "{\"fiProdId\":" + skuCatalogo + "}";
                    string respuestaWS = string.Empty;
                    ManejadorConsultaWS consWS = new ManejadorConsultaWS();
                    EntCatalogos catalogo = new EntCatalogos();
                    EntProductoCatalogoExtendido productoCatExt = new EntProductoCatalogoExtendido();
                    EntProductoCatalogoExtendido productoCE = new EntProductoCatalogoExtendido();
                    URL = this.RecuperaCatalogoGenerico(1681, 1);
                    ServicioEktComCatExt consultaServicio = new ServicioEktComCatExt();


                    //Se consulta la información del WS del producto de CatExt
                    if (URL != string.Empty)
                    {
                        oEntRespuesta.mensajeError = "URL: " + URL;
                    }
                    else
                    {
                        oEntRespuesta.eTipoError = EnumTipoErrorNST.Error;
                        oEntRespuesta.mensajeError = "No fue posible recuperar la URL para consulta central";
                        return oEntRespuesta;
                    }

                    productoCatExt = consultaServicio.ObtenerProductoCatalogoExtendido(Convert.ToString(skuCatalogo));

                    if (productoCatExt.fiProdId != 0)
                    {
                        productoCatExt.fiProdId = skuCatalogo;
                        oEntRespuesta.mensajeError = "Asignación exitosa. ";

                        if (precioSku != 0)
                            productoCatExt.fnProdPrecio = precioSku; //Asigna precio de Ekt.com

                        productoCatExt.RegistrarProductoCatalogoExt();
                        oEntRespuesta.mensajeError = "Producto CatExt registrado con éxito: " + productoCatExt.fiProdId;
                    }
                    else
                    {
                        oEntRespuesta.eTipoError = EnumTipoErrorNST.Error;
                        oEntRespuesta.mensajeError = "No se recuperó información del producto: " + skuCatalogo + ", no se agregó al carrito.";
                    }

                }
                catch (Exception exc)
                {
                    oEntRespuesta.eTipoError = EnumTipoErrorNST.Error;
                    oEntRespuesta.mensajeError = "Ocurrió un error al consultar producto CatExt: " + exc.Message;
                }
            }
            return oEntRespuesta;
        }

        public EntRespuestaNST ConsultarCoberturaOmnicanal(string CP, int sku, int pais, string canal, int tienda, bool validarInv)
        {
            EntRespuestaNST respuesta = new EntRespuestaNST();
            string respuestaCobertura = "0";
            int inventarioExh = 0;
            int inventarioBod = 0;
            bool hayInventario = false;
            try
            {
                string URL = this.RecuperaCatalogoGenerico(1681, 3);
                string carrier = "";
                string[] carriers;
                string descripcion;
                if (URL.Length == 0)
                {
                    respuesta.eTipoError = EnumTipoErrorNST.Error;
                    respuesta.mensajeError = "No se pudo recuperar la URL de WS para validar cobertura de Código Postal, favor de contactar a Soporte.";
                    return respuesta;
                }

                if (!validarInv)
                    carrier = "ektnvia";

                intfCoberturaservice servicioCobertura = new intfCoberturaservice(URL);
                respuestaCobertura = servicioCobertura.CoberturaOp("1", CP, carrier, 1, out descripcion, out carriers);

                //0 no hay cobertura, si hay cobertura 1
                if (respuestaCobertura == "1")
                {
                    respuesta.mensajeError = "Hay cobertura";
                }
                else
                {
                    respuesta.eTipoError = EnumTipoErrorNST.Warning;
                    respuesta.mensajeError = "No hay cobertura";
                    return respuesta;
                }
            }
            catch (Exception e)
            {
                //0 no hay cobertura, si hay cobertura 1
                if (respuestaCobertura != "1")
                {
                    respuesta.eTipoError = EnumTipoErrorNST.Error;
                    respuesta.mensajeError = "No hay cobertura: " + e.Message;
                    return respuesta;
                }
            }

            if (validarInv)
            {
                try
                {
                    //Se valida inventario
                    string URLInv = this.RecuperaCatalogoGenerico(1681, 4);
                    if (URLInv.Length == 0)
                    {
                        respuesta.eTipoError = EnumTipoErrorNST.Error;
                        respuesta.mensajeError = "No se pudo recuperar la URL de WS para validar inventario Catálogo Extendido, favor de contactar a Soporte.";
                        return respuesta;
                    }

                    intfBusquedaPorCodigoPostalservice servicioInventario = new intfBusquedaPorCodigoPostalservice(URLInv);
                    ObtieneInfTienda_SalidaDetalle[] respuestaInventario = servicioInventario.ObtieneInfTiendaOp(pais, canal, tienda, sku);
                    for (int i = 0; i < respuestaInventario.Length; i++)
                    {
                        if (respuestaInventario[i].Inventario_Exh == string.Empty)
                            inventarioExh = 0;
                        else
                            inventarioExh = Convert.ToInt32(respuestaInventario[i].Inventario_Exh);

                        if (respuestaInventario[i].Inventario_Exh == string.Empty)
                            inventarioBod = 0;
                        else
                            inventarioBod = Convert.ToInt32(respuestaInventario[i].Inventario_Bod);

                        if (inventarioExh > 0 || inventarioBod > 0)
                        {
                            hayInventario = true;
                            continue;
                        }
                        if (hayInventario)
                            continue;
                    }

                    if (!hayInventario)
                    {
                        respuesta.eTipoError = EnumTipoErrorNST.Error;
                        respuesta.mensajeError = "No hay inventario disponible para el sku:" + sku;
                    }
                }
                catch (Exception e)
                {

                    if (!hayInventario && validarInv)
                    {
                        respuesta.eTipoError = EnumTipoErrorNST.Error;
                        respuesta.mensajeError = "No hay inventario disponible para el sku:" + sku + ", " + e.Message;
                    }
                }
            }
            return respuesta;
        }

        /// <summary>
        /// Método para generar cadena de envío con los productos en una orden Vtex
        /// </summary>
        /// <param name="productos"></param>
        /// <returns></returns>
        public string GenerarCadenaProductoPeticion(List<EntOrdenProductosNST> productos)
        {
            string cadena = "[";

            for (int i = 0; i < productos.LongCount(); i++)
            {
                cadena = cadena + "{\"itemId\":\"" + productos[i].producto + "\",\"quantity\":" + productos[i].cantidad + "}";
            }

            cadena = cadena + "]";

            return cadena;

        }

        /// <summary>
        /// Método que recibe cadena y la cifra para venta de Captura de domicilio para Omnicanal
        /// </summary>
        /// <param name="cadenaToken"></param>
        /// <returns></returns>
        public EntRespuestaNST GenerarCifradoAESCatalogoExtendido(string cadenaToken, int idCifrado)
        {
            EntRespuestaNST oEntRespuesta = new EntRespuestaNST();
            try
            {
                string keyCifrado = this.RecuperaCatalogoGenerico(1681, idCifrado).Trim();
                if (keyCifrado.Length == 0)
                {
                    oEntRespuesta.eTipoError = EnumTipoErrorNST.Error;
                    oEntRespuesta.mensajeError = "No se recuperó correctamente el key de cifrado, favor de contactar a soporte";
                }
                else
                {
                    string key = keyCifrado;
                    string vector = keyCifrado;
                    ManejadorCifradoAES manejadorCifrado = new ManejadorCifradoAES(key, vector);

                    oEntRespuesta.mensajeError = manejadorCifrado.EncryptText(cadenaToken);
                }
            }
            catch (Exception e)
            {
                oEntRespuesta.eTipoError = EnumTipoErrorNST.Error;
                oEntRespuesta.mensajeError = e.Message;
            }

            return oEntRespuesta;
        }
        /// <summary>
        /// Método que recupera URL para Captura de domicilio para Omnicanal
        /// </summary>
        /// <param name="cadenaToken"></param>
        /// <returns></returns>
        public string ConsultarURLCapturaDomicilioOmnicanal()
        {
            string URL = string.Empty;
            try
            {
                URL = this.RecuperaCatalogoGenerico(1681, 9).Trim();
                if (URL.Length == 0)
                {
                    URL = "1,No se recuperó correctamente la URL";
                }
            }
            catch (Exception e)
            {
                URL = "1,Hubo un error al recuperar la URL de captura domicilio";
            }

            return URL;
        }

        /// <summary>
        /// Método que realiza el apartado de mercancía con Ecommerce
        /// </summary>
        /// <param name="orden">Recibe objeto de orden para la consulta</param>
        /// <returns></returns>
        public EntRespuestaNST GenerarOrdenVtexCatalogoExtendido(EntOrdenCatalogoExtendidoNST orden)
        {
            bool registroDomicilio = false;
            bool registroOrden = false;
            EntRespuestaNST oEntRespuesta = new EntRespuestaNST();
            string cadena = string.Empty;

            try
            {
                string URL = string.Empty;
                string respuestaWS = string.Empty;
                string folioVtex = string.Empty;
                string apellidos = orden.apellidoPat + " " + orden.apellidoMat;
                ManejadorConsultaWS consWS = new ManejadorConsultaWS();
                EntCatalogos catalogo = new EntCatalogos();
                EntRespuestaOrdenCatExt respuestaCatExt = new EntRespuestaOrdenCatExt();
                cadena = cadena + orden.presupuesto;
                URL = this.RecuperaCatalogoGenerico(1681, 6);

                cadena = cadena + URL;
                string productos = this.GenerarCadenaProductoPeticion(orden.datosProductos);
                //Borrar dígito 0
                string peticionWS = "{\"marketplaceOrderId\": \"" + orden.sucursal + "0" + orden.presupuesto + "\",\"itemsInOrder\": " + productos + ",\"clientProfileData\": {\"firstName\": \"" + orden.nombre + "\",\"lastName\": \"" + apellidos + "\",\"phone\": \"" + orden.telefono + "\"," + "\"email\": \"" + orden.correoE +
                   "\"},\"shippingData\": {\"receiverName\": \"" + orden.nombreRecibe + "\",\"postalCode\": \"" + orden.codPostal + "\",\"city\": \"" + orden.delegMuni + "\",\"state\": \"" + orden.estado + "\",\"street\": \"" + orden.direccion + "\",\"number\": \"" + orden.noExterior + "\",\"complement \": \"" + orden.noInterior +
                   "\",\"neighborhood\": \"" + orden.colonia + "\",\"reference\": \"" + orden.referencia + "\"}}";

                cadena = cadena + peticionWS;
                //Se consulta la información del WS del producto de CatExt
                if (URL != string.Empty)
                {
                    oEntRespuesta.mensajeError = "URL: " + URL;
                    cadena = cadena + ", URL: " + URL;
                }
                else
                {
                    oEntRespuesta.eTipoError = EnumTipoErrorNST.Error;
                    oEntRespuesta.mensajeError = "No fue posible recuperar la URL para enviar orden Vtex";
                    return oEntRespuesta;
                }

                respuestaWS = consWS.ConnectWS(URL, peticionWS, 60000);

                cadena = cadena + ", petición: " + peticionWS + ",respuestaWS: " + respuestaWS;
                oEntRespuesta.mensajeError = cadena;

                System.Diagnostics.Trace.WriteLine("cadena apartado Vtex: " + cadena, "LOG");

                respuestaCatExt = consWS.JScriptSerializa.Deserialize<EntRespuestaOrdenCatExt>(respuestaWS);

                if (respuestaCatExt.Result == null || respuestaCatExt.Result.OrderId == null)
                {
                    respuestaCatExt.Result = new EntResultadoOrdenCatExt();
                    respuestaCatExt.Result.OrderId = "0";
                    respuestaCatExt.Result.ActionDescription = respuestaCatExt.Message;
                }

                //Guardar número de Orden en BD local
                registroOrden = orden.GuardarRegistroOrdenVtex(1, respuestaCatExt.Result.OrderId, 0, peticionWS, respuestaWS);
                //Guardar información de cliente en BD local, proceso 5
                orden.proceso = 5;
                registroDomicilio = orden.GuardarDatosClienteOmnicanal();

                if (respuestaCatExt.Code == 0)
                    oEntRespuesta.mensajeError = "La orden se generó correctamente: " + respuestaCatExt.Result.OrderId;// + ", cadena:" + cadena;
                else
                {
                    oEntRespuesta.eTipoError = EnumTipoErrorNST.Error;
                    oEntRespuesta.mensajeError = "La orden no se generó correctamente: " + respuestaCatExt.Result.ActionDescription;// +", cadena:" + cadena;
                }

            }
            catch (Exception exc)
            {
                oEntRespuesta.eTipoError = EnumTipoErrorNST.Error;
                if (!registroOrden)
                    oEntRespuesta.mensajeError = "Ocurrió un error al guardar registro de Orden" + exc.Message; //+ ", cadena:" + cadena;
                else if (!registroDomicilio)
                    oEntRespuesta.mensajeError = "Ocurrió un error al registrar la información del cliente" + exc.Message;// + ", cadena:" + cadena;
                else
                    oEntRespuesta.mensajeError = " Ocurrió un error al registrar Orden CatExt: " + exc.Message;// +", cadena:" + cadena;

            }
            return oEntRespuesta;
        }

        /// <summary>
        /// Método que realiza el complemento de pago a Ecommerce para Catálogo Extendido
        /// </summary>
        /// <param name="orden">Recibe objeto de orden para la consulta</param>
        /// <returns></returns>
        public EntRespuestaNST GenerarConfirmacionVtexCatalogoExtendido(string presupuesto, double monto, string autorizacion, int tipoPago, string ordenVtex, string empleado)
        {
            System.Diagnostics.Trace.WriteLine("ENTRA A GenerarConfirmacionVtexCatalogoExtendido", "LOG");

            bool registroOrden = false;
            EntRespuestaNST oEntRespuesta = new EntRespuestaNST();
            string cadena = string.Empty;
            string respuestaWSTem = string.Empty;
            int noPedido = 0;

            try
            {
                string URL = string.Empty;
                string respuestaWS = string.Empty;

                ManejadorConsultaWS consWS = new ManejadorConsultaWS();
                EntCatalogos catalogo = new EntCatalogos();
                EntOrdenCatalogoExtendidoNST orden = new EntOrdenCatalogoExtendidoNST();
                EntRespuestaConfirmacionCatExt confirmacionCatExt = new EntRespuestaConfirmacionCatExt();
                cadena = cadena + ordenVtex;
                URL = this.RecuperaCatalogoGenerico(1681, 18);
                DateTime fecha = DateTime.Today;
                TimeSpan horaActual = fecha.TimeOfDay;
                string mes = fecha.Month > 9 ? Convert.ToString(fecha.Month) : "0" + Convert.ToString(fecha.Month);
                string dia = fecha.Day > 9 ? Convert.ToString(fecha.Day) : "0" + Convert.ToString(fecha.Day);
                string hora = horaActual.Hours > 9 ? Convert.ToString(horaActual.Hours) : "0" + Convert.ToString(horaActual.Hours);
                string minuto = horaActual.Minutes > 9 ? Convert.ToString(horaActual.Minutes) : "0" + Convert.ToString(horaActual.Minutes);
                string segundo = horaActual.Seconds > 9 ? Convert.ToString(horaActual.Seconds) : "0" + Convert.ToString(horaActual.Seconds);

                string peticionWS = "{\"info\": { "
                    + "\"estacionTrabajo\": \"SERVER\", "
                    + "\"idUsuario\": \"" + empleado + "\", ";

                EntOrdenCatalogoExtendidoNST entidadCliente = new EntOrdenCatalogoExtendidoNST();
                entidadCliente.ConsultarDatosClienteOmnicanal(presupuesto);
                string informacionCliente = "\"informacionCliente\": { \"ApellidoMaternoCliente\": \"" + entidadCliente.apellidoPat + "\", "
                    + "\"ApellidoPaternoCliente\": \"" + entidadCliente.apellidoMat + "\", \"Email\": \"" + entidadCliente.correoE + "\", \"NombreCliente\": \"" + entidadCliente.nombre + "\", "
                    + "\"NombreCompletoCliente\": \"" + entidadCliente.nombre + " " + entidadCliente.apellidoPat + " " + entidadCliente.apellidoMat + "\", "
                    + "\"domicilio\": { \"calle\": \"" + entidadCliente.direccion + "\", \"codigoPostal\": \"" + entidadCliente.codPostal + "\", "
                    + "\"colonia\": \"" + entidadCliente.colonia + "\", \"estado\": \"" + entidadCliente.estado + "\", \"numeroExterno\": \"" + entidadCliente.noExterior + "\", "
                    + "\"numeroInterno\": \"" + entidadCliente.noInterior + "\", \"poblacion\": \"" + entidadCliente.delegMuni + "\" } },";

                ManejadorPagosVenta manejadorPagos = new ManejadorPagosVenta();
                EntResponsePrespuestoInfo entidad = manejadorPagos.ConsultarInformacionPresupuesto(Convert.ToInt32(presupuesto), empleado);
                Elektra.Negocio.Entidades.PagosManejador.EntProductoDetalle detalle = new Elektra.Negocio.Entidades.PagosManejador.EntProductoDetalle();
                detalle = entidad.PresupuestoDetalle[0];

                string[] vtexTokens = ordenVtex.Split('-');

                string infromacionVenta = "\"informacionVenta\": [ {\"cantidad\": " + detalle.Cantidad + ", \"costoProducto\": " + detalle.ProdPrecio + ", "
                    + "\"descuento\": " + detalle.Descuento + ", \"lstMilenias\": [ {\"sku\": 0, \"sobreprecio\":0 } ], \"lstPromociones\": [ { \"cantidad\":1, \"idPromocion\":4939, \"idRegalo\":911, \"monto\":" + detalle.ProdPrecio + ", \"subTipoPromocion\":0, \"tipoPromocion\":32 } ], "
                    + "\"mecanica\": 0, \"precio\": " + detalle.ProdPrecio + ", \"sku\": " + detalle.ProductoId + ", \"sobreprecio\": 0, \"descuentoEspecial\": " + detalle.DescuentoEsp + " "
                    + "} ],"
                    + "\"referenciaEktCom\": \"" + vtexTokens[1] + "\", \"tipoVenta\":1, \"totalVenta\": " + entidad.MontoAPagar + ", "
                    + "\"informacionPago\": { \"tipoPago\": " + tipoPago + ", \"plazoMsi\":1, \"bancoId\": \"911\", \"numeroTarjeta\": \"\", \"referenciaPago\": \"" + vtexTokens[1] + "\", "
                    + "\"monto\": " + entidad.MontoAPagar + " "
                    + "}, "
                    + "\"consecutivoPedido\":1, "
                    + "\"costoEstimado\":0,"
                    + "\"tipoEntrega\":1, "
                    + "\"proveedorEnvio\": \"ESTAFEPAQ\", "
                    + "\"omnicanalId\":9, "
                    + "\"totalPedidos\":1, "
                    + "\"afectaConta\":1, "
                    + "\"marketPlaceId\":0, "
                    + "\"sellerId\":0, "
                    + "\"montoVentaMP\":0, "
                    + "\"montoComision\":0, "
                    + "\"penalizacionId\":0, "
                    + "\"montoPenalizacion\":0, "
                    + "\"origenEnvioId\":1, "
                    + "\"pedidoOtorgaRegalo\":0, "
                    + "\"skuOtorgaRegalo\":0, "
                    + "\"tiendaSurt\":0 "
                    + "}, \"paymentcomplement\":";

                string peticion = "{\"amount\": \"" + monto + "\",\"authorizationId\": \"" + autorizacion + "\",\"paymentType\": \""
                    + tipoPago + "\",\"reference\": \"" + ordenVtex + "\",\"paymentDate\": \"" + fecha.Year + "-" + mes + "-" + dia + "T" + hora + ":" + minuto + ":" + segundo//+""+"2019-02-26T15:03:00"
                    + "\",\"employeeId\": \"" + empleado + "\"} }";

                System.Diagnostics.Trace.WriteLine("Cadena nueva " + peticionWS + informacionCliente + infromacionVenta + peticion, "LOG");

                peticionWS = peticionWS + informacionCliente + infromacionVenta + peticion;

                cadena = cadena + peticionWS;
                //Se consulta la información del WS del producto de CatExt
                if (URL != string.Empty)
                {
                    oEntRespuesta.mensajeError = "URL: " + URL;
                    cadena = cadena + ", URL: " + URL;
                }
                else
                {
                    oEntRespuesta.eTipoError = EnumTipoErrorNST.Error;
                    oEntRespuesta.mensajeError = cadena + "No fue posible recuperar la URL para enviar orden Vtex" + ", peticionWS:" + peticionWS;
                    return oEntRespuesta;
                }

                respuestaWSTem = consWS.ConnectWS(URL, peticionWS, 60000);
                System.Diagnostics.Trace.WriteLine("Respuesta nueva " + respuestaWSTem, "LOG");

                cadena = cadena + ", petición: " + peticionWS + ",respuestaWSTem: " + respuestaWSTem;
                oEntRespuesta.mensajeError = cadena;
                respuestaWS = respuestaWSTem.Replace("\"Message\":null", "\"Message\":\"Sin Error\"");
                cadena = cadena + ", respuesta sin nulos: " + respuestaWS;


                confirmacionCatExt = consWS.JScriptSerializa.Deserialize<EntRespuestaConfirmacionCatExt>(respuestaWS);

                if (confirmacionCatExt.Message == null)
                    confirmacionCatExt.Message = string.Empty;

                cadena = " Deserialize correcto: " + cadena;
                //Guardar número de Orden en BD local
                if (confirmacionCatExt.Code == 0 && confirmacionCatExt.Result.LongCount() > 0) //No hubo error.
                    noPedido = confirmacionCatExt.Result[0].noVenta;


                cadena = "noPedido: " + noPedido + ", " + cadena;

                if (confirmacionCatExt.Code == 0)
                {
                    oEntRespuesta.mensajeError = Convert.ToString(noPedido); //Pedido Ekt.com generado.
                }
                else if (confirmacionCatExt.Code == 636679425875416252)
                {
                    oEntRespuesta.eTipoError = EnumTipoErrorNST.Warning;
                    //Realiza cancelación de la orden.
                    EntRespuestaNST errorCancelacion = GenerarCancelacionVtexCatalogoExtendido(ordenVtex, Convert.ToInt32(presupuesto));

                    if (errorCancelacion.eTipoError == 0)
                        oEntRespuesta.mensajeError = "La confirmación de orden Vtex no se generó correctamente, mensaje: " + confirmacionCatExt.Message + " se realizó la cancelación de la Orden de Catálogo Extendido correctamente.";
                    else
                        oEntRespuesta.mensajeError = "La confirmación de orden Vtex no se generó correctamente, mensaje: " + confirmacionCatExt.Message + ", al realizar la cancelación de la Orden de Catálogo Extendido se recibió el error: " + errorCancelacion.mensajeError;
                }
                else
                {
                    oEntRespuesta.eTipoError = EnumTipoErrorNST.Error;
                    oEntRespuesta.mensajeError = "La confirmación de Orden de Catálogo Extendido no se generó correctamente: " + confirmacionCatExt.Message;
                }

                cadena = oEntRespuesta.mensajeError + ", cadena:" + cadena;
                cadena = cadena + "proceso 2, string folioVtex: " + ordenVtex;
                cadena = cadena + ", int pedidoCom: " + noPedido + ", string peticion:" + peticionWS + ", string respuesta:" + respuestaWS;

                orden.presupuesto = presupuesto;
                registroOrden = orden.GuardarRegistroOrdenVtex(2, ordenVtex, noPedido, peticionWS, respuestaWS);

            }
            catch (Exception exc)
            {
                oEntRespuesta.eTipoError = EnumTipoErrorNST.Error;
                if (!registroOrden && noPedido > 0)
                    oEntRespuesta.mensajeError = noPedido.ToString();
                else if (respuestaWSTem.Length > 0)
                    oEntRespuesta.mensajeError = "Ocurrió un error al registrar Orden de Catálogo Extendido: " + exc.Message;
                else
                    oEntRespuesta.mensajeError = "Ocurrió un error al realizar la confirmación de pago de Orden de Catálogo Extendido: " + exc.Message;

            }
            return oEntRespuesta;
        }

        /// <summary>
        /// Cancelar 
        /// </summary>
        /// <param name="ordenVtex"></param>
        /// <param name="presupuesto"></param>
        /// <returns></returns>
        public EntRespuestaNST GenerarCancelacionVtexCatalogoExtendido(string ordenVtex, int presupuesto)
        {
            EntRespuestaNST oEntRespuesta = new EntRespuestaNST();
            string cadena = string.Empty;
            bool registroOrden = false;

            try
            {
                string URL = string.Empty;
                string respuestaWS = string.Empty;
                ManejadorConsultaWS consWS = new ManejadorConsultaWS();
                EntCatalogos catalogo = new EntCatalogos();
                URL = this.RecuperaCatalogoGenerico(1681, 8);
                EntRespuestaCancelacionCatExt oRespuestaWS = new EntRespuestaCancelacionCatExt();
                EntOrdenCatalogoExtendidoNST orden = new EntOrdenCatalogoExtendidoNST();

                string peticionWS = "{\"fcReference\": \"" + ordenVtex + "\"}";

                cadena = cadena + peticionWS;
                if (URL != string.Empty)
                {
                    oEntRespuesta.mensajeError = "URL: " + URL;
                    cadena = cadena + ", URL: " + URL;
                }
                else
                {
                    oEntRespuesta.eTipoError = EnumTipoErrorNST.Error;
                    oEntRespuesta.mensajeError = cadena + "No fue posible recuperar la URL para enviar Orden de Catálogo Extendido" + ", peticionWS:" + peticionWS;
                    return oEntRespuesta;
                }

                respuestaWS = consWS.ConnectWS(URL, peticionWS, 60000);

                oRespuestaWS = consWS.JScriptSerializa.Deserialize<EntRespuestaCancelacionCatExt>(respuestaWS);

                if (oRespuestaWS.Code == 0)
                {
                    oEntRespuesta.mensajeError = "Se canceló correctamente Orden de Catálogo Extendido:" + oRespuestaWS.Message;
                }
                else
                {
                    oEntRespuesta.eTipoError = EnumTipoErrorNST.Error;
                    oEntRespuesta.mensajeError = "Hubo un error al cancelar Orden de Catálogo Extendido:" + oRespuestaWS.Message;
                }

                //Registro proceso 3 cancelación.
                orden.presupuesto = Convert.ToString(presupuesto);
                registroOrden = orden.GuardarRegistroOrdenVtex(3, ordenVtex, 0, peticionWS, respuestaWS);

            }
            catch (Exception exc)
            {
                oEntRespuesta.eTipoError = EnumTipoErrorNST.Error;
                oEntRespuesta.mensajeError = exc.Message;
            }

            return oEntRespuesta;
        }
        public EntRespuestaNST GenerarImpresionTicketOmcCatExt(int pedido, int tipoVenta, string folioVtex)
        {
            EntRespuestaNST oEntRespuesta = new EntRespuestaNST();
            try
            {
                TicketOmcCatExt impresion = new TicketOmcCatExt();
                oEntRespuesta.mensajeError = impresion.ImprimeTicketOmcCatExt(pedido, tipoVenta, folioVtex);
            }
            catch (Exception exc)
            {
                oEntRespuesta.eTipoError = EnumTipoErrorNST.Error;
                oEntRespuesta.mensajeError = "No se pudo recuperar la impresión de Promesa de entrega para Catálogo Extendido" + exc.Message;
            }
            return oEntRespuesta;
        }

        public EntResultadoSurtimientoCatExtNST GenerarSurtimientoCatExt(int pedido, string usuario, int tienda, string estacion, string password, string FolioVtex)
        {
            Resultado resultado = new Resultado();
            EntResultadoSurtimientoCatExtNST resultadoSurt = new EntResultadoSurtimientoCatExtNST();
            try
            {
                WSSurtimientoContado oWSSurtimientoContado = new WSSurtimientoContado();
                resultado = oWSSurtimientoContado.AplicarSurtimientoCatalogoExtendido(pedido, true, usuario, tienda, true, estacion, password, FolioVtex);

                resultadoSurt.Respuesta = resultado.Respuesta;
                resultadoSurt.MensajeTecnico = resultado.MensajeTecnico;
            }
            catch (Exception exc)
            {
                resultado.MensajeTecnico = exc.Message;
            }
            return resultadoSurt;
        }

        #endregion

        #region EnvioDomicilio

        /// <summary>
        /// Método que consulta el CeDis que corresponde a la tienda actual.
        /// </summary>
        /// <param name="tienda"></param>
        /// <returns></returns>
        private int ConsultarCeDisCorrespondeEnvioDomicilio(int tienda, int sku)
        {
            int respuestaRelacion = 0;
            string descrip = string.Empty;
            try
            {
                string URL = this.RecuperaCatalogoGenerico(1681, 12);

                if (URL.Length == 0)
                {
                    ApplicationException ApEx = new ApplicationException("No se pudo recuperar la URL de WS para obtener relación Tienda - CeDis, favor de contactar a Soporte.");
                    throw ApEx;
                }

                //intfInventario_vtsservice servicioRelacion = new intfInventario_vtsservice(URL);
                intfRELACION_TIENDASservice servicioRelacion = new intfRELACION_TIENDASservice(URL);
                System.Diagnostics.Trace.WriteLine("Antes de llamada Cedis RELACION_sp_TIENDASOp", "LOG");
                int respuesta = servicioRelacion.RELACION_sp_TIENDASOp(1, tienda.ToString(), "1", out descrip);
                System.Diagnostics.Trace.WriteLine("Después de llamada Cedis RELACION_sp_TIENDASOp" + respuesta.ToString(), "LOG");

                if (respuesta != -1)
                    respuestaRelacion = respuesta;

                /* for (int i = 0; i < respuesta.Length; i++)
                 {
                     System.Diagnostics.Trace.WriteLine("El CeDis asignado es: respuesta[i] " + respuesta[i].ToString(), "LOG");
                     if (respuesta[i].Notienda != string.Empty)
                         respuestaRelacion = Convert.ToInt32(respuesta[i].Notienda);
                 }
                 */
                //0 no hay Cedis, si hay CeDis el número que corresponde
                if (respuestaRelacion != 1)
                {
                    System.Diagnostics.Trace.WriteLine("No hay un CeDis asignado a la sucursal:" + tienda + ", respuestaRelacion:" + respuestaRelacion + ", " + descrip, "LOG");
                    ApplicationException ApEx = new ApplicationException("No hay un CeDis asignado a la sucursal:" + tienda + ", " + descrip);
                    throw ApEx;
                }
                else
                {
                    //Se devuelve el CeDis correspondiente a la tienda actual
                    System.Diagnostics.Trace.WriteLine("El CeDis asignado es: " + respuestaRelacion, "LOG");
                }
            }
            catch (Exception e)
            {
                //0 no hay cobertura, si hay cobertura 1
                if (respuestaRelacion == 0)
                {
                    ApplicationException ApEx = new ApplicationException("No se pudo consultar el CeDis asignado a la tienda: " + e.Message);
                    throw ApEx;
                }
            }
            return respuestaRelacion;
        }

        /// <summary>
        /// Consulta inventario sobre el SKU disponible para la tienda
        /// </summary>
        /// <param name="tienda"></param>
        /// <returns></returns>
        public EntRespuestaNST ConsultarInventarioEnvioDomicilio(int pais, string canal, int tienda, int sku)
        {
            bool hayInventario = false;
            int inventarioCD = 0;
            EntRespuestaNST respuesta = new EntRespuestaNST();
            string relacionCeDis = "138";
            string URLInv = this.RecuperaCatalogoGenerico(1681, 17);

            try
            {

                //relacionCeDis = this.ConsultarCeDisCorrespondeEnvioDomicilio(tienda, sku);

                //Se valida inventario
                if (URLInv.Length == 0)
                {
                    respuesta.eTipoError = EnumTipoErrorNST.Error;
                    respuesta.mensajeError = "No se pudo recuperar la URL de WS para validar inventario Catálogo Extendido, favor de contactar a Soporte.";
                    return respuesta;
                }

                intfCONSULTA_INVENTARIO_VCDservice servicioInventario = new intfCONSULTA_INVENTARIO_VCDservice(URLInv);

                System.Diagnostics.Trace.WriteLine("Antes CONSULTA__INVENTARIO__VCDOp(" + relacionCeDis + "," + sku.ToString() + ")", "LOG");
                ParametrosDtl[] respuestaInventario = servicioInventario.CONSULTA__INVENTARIO__VCDOp(relacionCeDis, sku.ToString()); //Pendiente
                System.Diagnostics.Trace.WriteLine("Después CONSULTA__INVENTARIO__VCDOp(" + relacionCeDis + "," + sku.ToString() + ")", "LOG");

                //ObtieneInfTienda_SalidaDetalle[] respuestaInventario = servicioInventario.ObtieneInfTiendaOp(pais, canal, tienda, sku);
                for (int i = 0; i < respuestaInventario.Length; i++)
                {
                    if (respuestaInventario[i].QTY == string.Empty)
                        inventarioCD = 0;
                    else
                        inventarioCD += Convert.ToInt32(respuestaInventario[i].QTY);


                    if (inventarioCD > 0)
                    {
                        hayInventario = true;
                    }
                }

                if (!hayInventario)
                {
                    respuesta.eTipoError = EnumTipoErrorNST.Error;
                    respuesta.mensajeError = "No hay inventario disponible para el sku:" + sku;
                }
                else
                {
                    respuesta.mensajeError = inventarioCD.ToString();
                }

            }
            catch (Exception e)
            {
                if (!hayInventario)
                {
                    respuesta.eTipoError = EnumTipoErrorNST.Error;
                    respuesta.mensajeError = "No hay inventario disponible para el sku:" + sku + ", inventario: " + inventarioCD + ", error: " + e.Message + ", URL: " + URLInv;

                }
            }
            return respuesta;
        }


        /// <summary>
        /// Método para genera objeto de envío con los productos para Envío a Domicilio.
        /// </summary>
        /// <param name="idPresupuesto"></param>
        /// <param name="productos"></param>
        /// <param name="precioVenta"></param>
        /// <returns></returns>
        public ParametrosDetalle[] GenerarDetalleProductoPeticion(string idPresupuesto, List<EntOrdenProductosNST> productos, double precioVenta)
        {
            ParametrosDetalle[] detProductos = new ParametrosDetalle[productos.Count];

            for (int i = 0; i < productos.Count; i++)
            {
                detProductos[i] = new ParametrosDetalle();
                detProductos[i].ADN = idPresupuesto;
                detProductos[i].Rowid = "1";
                detProductos[i].Sku = productos[i].producto;
                detProductos[i].Qty = productos[i].cantidad.ToString();
                detProductos[i].Preventa = "N";
                detProductos[i].PrecioVta = precioVenta.ToString();
                detProductos[i].Carrier = "DHL";
            }
            return detProductos;
        }

        /// <summary>
        /// Método que realiza el apartado de mercancía en CeDis para enviar a domicilio del cliente.
        /// </summary>
        /// <param name="orden"></param>
        /// <param name="precioVenta"></param>
        /// <returns></returns>
        public EntRespuestaNST GenerarApartadoEnvioDomicilio(EntOrdenCatalogoExtendidoNST orden, double precioVenta)
        {
            EntRespuestaNST respuesta = new EntRespuestaNST();
            EntOrdenEnvioDomicilio entEnvDom = new EntOrdenEnvioDomicilio();
            decimal respuestaApartado = 0;
            bool registroApartado = false;
            bool resgistroCliente = false;
            try
            {
                string URL = this.RecuperaCatalogoGenerico(1681, 11);

                if (URL.Length == 0)
                {
                    respuesta.eTipoError = EnumTipoErrorNST.Error;
                    respuesta.mensajeError = "No se pudo recuperar la URL de WS para apartado de Envío a Domicilio, favor de contactar a Soporte.";
                    return respuesta;
                }
                intfWS_COM_EDservice servicioApartado = new intfWS_COM_EDservice(URL);

                char[] separador = new char[] { '-' };
                string[] estadoCeDis = orden.estado.Split(separador, 2);

                DateTime fecha = DateTime.Today;
                TimeSpan horaActual = fecha.TimeOfDay;
                string mes = fecha.Month > 9 ? Convert.ToString(fecha.Month) : "0" + Convert.ToString(fecha.Month);
                string dia = fecha.Day > 9 ? Convert.ToString(fecha.Day) : "0" + Convert.ToString(fecha.Day);
                string fechaEntrega = dia + "/" + mes + "/" + fecha.Year;

                //System.Diagnostics.Trace.WriteLine("orden.presupuesto: " + orden.presupuesto + ", precioVenta: " + precioVenta, "LOG");
                ParametrosDetalle[] detProductos = this.GenerarDetalleProductoPeticion(orden.presupuesto, orden.datosProductos, precioVenta);
                string Mensaje1 = string.Empty;
                string Mensaje2 = string.Empty;
                string Mensaje3 = string.Empty;
                string Mensaje4 = string.Empty;
                string peticionWS = string.Empty;

                //System.Diagnostics.Trace.WriteLine("Asignación", "LOG");

                if (orden.apellidoPat.Length == 0)
                    orden.apellidoPat = " ";

                if (orden.apellidoMat.Length == 0)
                    orden.apellidoMat = " ";


                peticionWS = "Tipo_Operacion: 1" + ", Pais: " + Convert.ToDecimal(orden.pais) + ", Canal: " + Convert.ToDecimal(orden.canal) + ", Store_nbr: " + orden.sucursal + ", Vendedor: " + orden.vendedor + ", Pedido: " + orden.presupuesto +
               ", Tipped: " + "EPOSED" + ", Nombre: " + orden.nombre + ", Apepcl: " + orden.apellidoPat + ", Apemcl: " + orden.apellidoMat + ", Compania:" + "EKTEPOS" + ", Aliasdir: " + "NT" + ", NombreDest: " + orden.nombreDest + ", ApPatDest:" + orden.apParternoDest +
               ", ApMatDest:" + orden.apMarternoDest + ", Calle: " + orden.direccion + ", Numero:" + orden.noExterior + ", Numeroint: " + orden.noInterior + ", Colonia:" + orden.colonia + ", Delegacion: " + orden.delegMuni + ", Ciudad: " + estadoCeDis[1] +
               ", Estado: " + estadoCeDis[0] + ", Cp: " + orden.codPostal + ", Referencias: " + orden.referencia + ", Latitud: " + orden.latitud + ", Longitud: " + orden.longitud + ", Telefonoc:" + orden.telefono + ", Telcel:" + "" + ", Telefonot: " + orden.telefono +
               ", Fechentrega: " + fechaEntrega + ", So_Type: " + "NO CONF" + ", Tipclient: 1, Tipenvio: 1, Carrier: EKTENVIA, Cancelar: N, persona1: NT, persona2: NT, Observaciones: " + orden.observaciones + ", FechaEpos: " + fechaEntrega + ", Email:" + orden.correoE +
               ", FechaPago: " + fechaEntrega + ", Detalle[ADN:" + orden.presupuesto + ", Rowid: 1, Sku: " + detProductos[0].Sku + "Qty: 1, Preventa: N, PrecioVta: " + detProductos[0].PrecioVta + ", Carrier: DHL]";

                System.Diagnostics.Trace.WriteLine("Asignación exitosa servicioApartado.WS__COM__EDOp", "LOG");
                System.Diagnostics.Trace.WriteLine(peticionWS, "LOG");

                respuestaApartado = servicioApartado.WS__COM__EDOp("1", Convert.ToDecimal(orden.pais), Convert.ToDecimal(orden.canal), orden.sucursal, orden.vendedor, Convert.ToDecimal(orden.presupuesto), "EPOSED", orden.nombre, orden.apellidoPat, orden.apellidoMat, "EKTEPOS", "NT", orden.nombreDest, orden.apParternoDest, orden.apMarternoDest,
                    orden.direccion, orden.noExterior, orden.noInterior, orden.colonia, orden.delegMuni, estadoCeDis[1], estadoCeDis[0], orden.codPostal, orden.referencia, orden.latitud, orden.longitud, orden.telefono, "", orden.telefono, fechaEntrega, "NO CONF", "1", "1", "EKTENVIA", "N", "NT", "NT", orden.observaciones, fechaEntrega, orden.correoE,
                    fechaEntrega, detProductos, out Mensaje1, out Mensaje2, out Mensaje3, out Mensaje4);

                System.Diagnostics.Trace.WriteLine("Después servicioApartado.WS__COM__EDOp: respuestaApartado-" + respuestaApartado, "LOG");
                //0 no hay cobertura, si hay cobertura 1
                if (respuestaApartado == 0 || respuestaApartado == -1)
                {
                    respuesta.eTipoError = EnumTipoErrorNST.Warning;
                    System.Diagnostics.Trace.WriteLine("No se colocó correctamente la orden, 1: " + Mensaje1 + "2: " + Mensaje2 + "3: " + Mensaje3 + "4: " + Mensaje4, "LOG");
                    respuesta.mensajeError = "No se colocó correctamente la orden, 1: " + Mensaje1 + "2: " + Mensaje2 + "3: " + Mensaje3 + "4: " + Mensaje4;
                }
                else
                {
                    respuesta.mensajeError = "Se colocó correctamente la orden, 1: " + Mensaje1 + "2: " + Mensaje2 + "3: " + Mensaje3 + "4: " + Mensaje4;
                }

                //Guardar bitácora en BD local (string presupuesto, string folioEAD, int estatus, string peticion, string respuesta)
                registroApartado = entEnvDom.GuardarProcesoBitacoraEnvDom(orden.presupuesto, orden.sucursal + orden.presupuesto, 1, peticionWS, "Respuesta: " + respuestaApartado.ToString() + ", Mensaje1: " + Mensaje1 + ", Mensaje2: " + Mensaje2 + ", Mensaje3: " + Mensaje3 + ", Mensaje4: " + Mensaje4);
                //Guardar información de cliente en BD local, proceso 4 
                orden.proceso = 4;
                resgistroCliente = orden.GuardarDatosClienteOmnicanal();

            }
            catch (Exception e)
            {
                //0 no hay cobertura, si hay cobertura 1
                if (respuestaApartado == 0)
                {
                    respuesta.eTipoError = EnumTipoErrorNST.Error;
                    respuesta.mensajeError = "Error al generar apartado de mercancía Envío a domicilio. Mensaje: " + e.Message;
                    System.Diagnostics.Trace.WriteLine("Error al generar apartado de mercancía Envío a domicilio. Mensaje: " + e.Message + " Trace: " + e.StackTrace, "LOG");

                }
                else
                    if (!registroApartado)
                    {
                        respuesta.eTipoError = EnumTipoErrorNST.Error;
                        respuesta.mensajeError = "Error al guardar registro de apartado de mercancía Envío a domicilio. Mensaje: " + e.Message;
                        System.Diagnostics.Trace.WriteLine("Error al generar bítacora de apartado de mercancía Envío a domicilio. Mensaje: " + e.Message + " Trace: " + e.StackTrace, "LOG");
                    }
                    else if (!resgistroCliente)
                    {
                        respuesta.eTipoError = EnumTipoErrorNST.Error;
                        respuesta.mensajeError = "Error al guardar registro de cliente de Envío a domicilio. Mensaje: " + e.Message;
                        System.Diagnostics.Trace.WriteLine("Error al guardar registro de cliente de Envío a domicilio. Mensaje: " + e.Message + " Trace: " + e.StackTrace, "LOG");
                    }
            }

            return respuesta;
        }





        private string GetIpAddress()
        {
            string ip = string.Empty;
            string ipLocal = "vacía";
            string cadena = string.Empty;
            char[] separador = new char[] { ':' };
            string[] ipCadena;

            string name = System.Net.Dns.GetHostName();
            for (int i = 0; i < System.Net.Dns.GetHostAddresses(name).Length; i++)
            {
                cadena = cadena + System.Net.Dns.GetHostAddresses(name)[i];
                System.Diagnostics.Trace.WriteLine("ip's GetHostAddresses: " + System.Net.Dns.GetHostAddresses(name)[i], "LOG");
            }
            if (System.Net.Dns.GetHostAddresses(name).Length > 0)
            {
                ip = System.Net.Dns.GetHostAddresses(name)[0].ToString();
                ipCadena = ip.Split(separador, 2);
                if (ipCadena.Length > 0)
                    ipLocal = ipCadena[0];
            }

            if (ipLocal == string.Empty)
                ipLocal = "127.0.0.1";
            return ipLocal;
        }

        public string WsTrackingOmnicanal(int presupuesto, string sucursal)
        {
            string json = string.Empty, body = string.Empty;
            JavaScriptSerializer jsSerializer = new JavaScriptSerializer();

            try
            {
                //Metodo para burlar certificado
                this.TrustAllCert();

                string URL = this.RecuperaCatalogoGenerico(1681, 13);

                if (URL.Length == 0)
                {
                    ApplicationException ApEx = new ApplicationException("No se pudo recuperar la URL de WS de Tracking");
                    throw ApEx;
                }

                var httpWebRequest = (HttpWebRequest)WebRequest.Create(URL);
                httpWebRequest.ContentType = "application/json";
                httpWebRequest.Method = "POST";

                using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
                {

                    EntOmnicanalTrackingNST ObjTrack = new EntOmnicanalTrackingNST();
                    string usuario = this.RecuperaCatalogoGenerico(1681, 14);
                    EntRespuestaNST cifrado = this.GenerarCifradoAESCatalogoExtendido(usuario, 10);

                    if (cifrado.eTipoError == EnumTipoErrorNST.SinError)
                        ObjTrack.usuario = cifrado.mensajeError;
                    else
                    {
                        ApplicationException ApEx = new ApplicationException("No ser recuperó correctamente el usuario");
                        throw ApEx;
                    }


                    //Consultar presupuesto

                    string token = sucursal + "@" + presupuesto;
                    cifrado = this.GenerarCifradoAESCatalogoExtendido(token, 10);
                    if (cifrado.eTipoError == EnumTipoErrorNST.SinError)
                    {
                        ObjTrack.token = cifrado.mensajeError;

                        System.Diagnostics.Trace.WriteLine("Token cifrado: " + ObjTrack.token, "LOG");
                    }
                    else
                    {
                        ApplicationException ApEx = new ApplicationException("No ser recuperó correctamente el usuario, token");
                        throw ApEx;
                    }

                    ObjTrack.ipEquipo = this.GetIpAddress();
                    ObjTrack.datosTracking.pais = 1;
                    ObjTrack.datosTracking.canal = 1;
                    ObjTrack.datosTracking.sucursal = Convert.ToInt32(sucursal);
                    ObjTrack.datosTracking.noPresupuesto = presupuesto;
                    ObjTrack.datosTracking.origen = "EAD";

                    json = jsSerializer.Serialize(ObjTrack);

                    streamWriter.Write(json);
                    streamWriter.Flush();
                    streamWriter.Close();
                }

                var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();

                StreamReader reader = new StreamReader(httpResponse.GetResponseStream());
                body = reader.ReadToEnd();

            }
            catch (Exception ex)
            {
                return ex.Message;
            }
            return body;

        }

        #endregion

        #region PublicoBonoRegalo

        public EntRespProductoBono ConsultaProductosCanjeBonoRegalo(string IdPromocionBono)
        {
            EntRespProductoBono respBono = new EntRespProductoBono();
            EntConsultasBDNST condb = new EntConsultasBDNST();
            bool RecuperoProductos = false;
            ArrayList LstSKuCanje = new ArrayList();
            try
            {

                string[] lstPromoID = IdPromocionBono.Split(',');

                foreach (string idProm in lstPromoID)
                {
                    EntAgrupaProductoBono agruparBonos = new EntAgrupaProductoBono();

                    DataSet dsProd = condb.ObtenerProductosCanjeBonoRegalo(int.Parse(idProm));

                    if (dsProd != null && dsProd.Tables != null && dsProd.Tables.Count > 0 && dsProd.Tables[0].Rows.Count > 0)
                    {
                        RecuperoProductos = true;
                        for (int i = 0; i < dsProd.Tables[0].Rows.Count; i++)
                        {
                            EnProductoBonoRegalo productoCanje = new EnProductoBonoRegalo();
                            productoCanje.Cantidad = int.Parse(dsProd.Tables[0].Rows[i]["fiInventario"].ToString());
                            productoCanje.DescProducto = dsProd.Tables[0].Rows[i]["fcProdDesc"].ToString();
                            productoCanje.PrecioProducto = double.Parse(dsProd.Tables[0].Rows[i]["fnProdPrecio"].ToString());
                            productoCanje.SKU = int.Parse(dsProd.Tables[0].Rows[i]["fiProdId"].ToString());

                            //if (!LstSKuCanje.Contains(productoCanje.SKU))
                            //{
                            //    LstSKuCanje.Add(productoCanje.SKU);
                            agruparBonos.LstProductoBono.Add(productoCanje);
                            //}
                        }
                    }
                    agruparBonos.IdPromocinBono = int.Parse(idProm);
                    respBono.LstProductoBono.Add(agruparBonos);
                }

                if (!RecuperoProductos)
                {
                    System.Diagnostics.Trace.WriteLine("No se pudieron recupear los productos de canje para las siguientes promocines. IDSPromo: " + IdPromocionBono, "LOG");
                    respBono.LstProductoBono = new List<EntAgrupaProductoBono>();
                    respBono.oEntRespuesta.eTipoError = EnumTipoErrorNST.Error;
                    respBono.oEntRespuesta.mensajeError = "Ocurrio un error al consultar los productos para el canje de bono regalo.";
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine("Ocurrio un error al consultar los productos para el canje de bono regalo. IDSPromo: " + IdPromocionBono, "LOG");
                System.Diagnostics.Trace.WriteLine("Message: " + ex.Message + " --- StackTrace: " + ex.StackTrace, "LOG");
                respBono.LstProductoBono = new List<EntAgrupaProductoBono>();
                respBono.oEntRespuesta.eTipoError = EnumTipoErrorNST.Error;
                respBono.oEntRespuesta.mensajeError = "Ocurrio un error al consultar los productos para el canje de bono regalo.";
            }
            return respBono;
        }

        #endregion

        #region PrivadoBono
        private DetalleVentaBonoNST[] CrearDetalleVentaBono(List<EntDetalleVentaBaseNST> lstEntDetalleVentaBaseNST, bool esGrabaPresupuesto)
        {
            List<DetalleVentaBonoNST> lstDettBonoAux = new List<DetalleVentaBonoNST>();
            DetalleVentaBonoNST[] lstDetalleVentaBono;

            for (int i = 0; i < lstEntDetalleVentaBaseNST.Count; i++)
            {
                if (lstEntDetalleVentaBaseNST[i].eTipoAgregadoNST == EnumTipoAgregadoNST.PromocionBono)
                {
                    if (lstEntDetalleVentaBaseNST[i].PrecioSugerido <= 0)
                        throw new ApplicationException("Warning : No se pudo recuperar el precio del roducto " + lstEntDetalleVentaBaseNST[i].SKU.ToString() + " Para el canje del Bono.");

                    DetalleVentaBonoNST oDetalleVentaBono = new DetalleVentaBonoNST();
                    oDetalleVentaBono.MontoSobrePrecio = lstEntDetalleVentaBaseNST[i].montoSobreprecio;
                    oDetalleVentaBono.SKU = lstEntDetalleVentaBaseNST[i].SKU;
                    oDetalleVentaBono.tipoPromocion = EnumTipoPromocionNST.Elektrapesos;
                    oDetalleVentaBono.IdBonoAgregado = lstEntDetalleVentaBaseNST[i].IdBonoAgregado;
                    oDetalleVentaBono.PrecioLista = lstEntDetalleVentaBaseNST[i].PrecioSugerido;
                    lstDettBonoAux.Add(oDetalleVentaBono);
                    lstEntDetalleVentaBaseNST.Remove(lstEntDetalleVentaBaseNST[i]);
                    i--;
                }
            }

            if (lstDettBonoAux.Count > 0)
                lstDetalleVentaBono = lstDettBonoAux.ToArray();
            else
                lstDetalleVentaBono = new DetalleVentaBonoNST[0];

            return lstDetalleVentaBono;
        }

        private DetalleVentaBono[] LlenarProductosBonoPorID(ref DetalleVentaBonoNST[] LstProdBonosID, string IDBono)
        {
            List<DetalleVentaBono> lstDettBonoAux = new List<DetalleVentaBono>();
            DetalleVentaBono[] lstDetalleVentaBono;

            if (LstProdBonosID.Length == 0)
                return lstDetalleVentaBono = new DetalleVentaBono[0];

            string[] lstPromoID = IDBono.Split(',');

            foreach (string idProm in lstPromoID)
            {
                List<DetalleVentaBonoNST> lstDettBonoNSTAux = new List<DetalleVentaBonoNST>();
                for (int i = 0; i < LstProdBonosID.Length; i++)
                {
                    if (LstProdBonosID[i].IdBonoAgregado == int.Parse(idProm))
                    {
                        DetalleVentaBono oDetalleVentaBono = new DetalleVentaBono();
                        oDetalleVentaBono.MontoSobrePrecio = 0;
                        oDetalleVentaBono.PrecioLista = LstProdBonosID[i].PrecioLista;
                        oDetalleVentaBono.SKU = LstProdBonosID[i].SKU;
                        oDetalleVentaBono.Descripcion = LstProdBonosID[i].Descripcion;
                        oDetalleVentaBono.tipoPromocion = EnumTipoPromocion.Elektrapesos;
                        oDetalleVentaBono.IdBonoAgregado = LstProdBonosID[i].IdBonoAgregado;
                        oDetalleVentaBono.lstAtributos = new Atributo[0];
                        oDetalleVentaBono.lstMileniasSeleccionadas = new EntMileniaSeleccionada[0];
                        lstDettBonoAux.Add(oDetalleVentaBono);

                        oDetalleVentaBono.IdBonoAgregadoSpecified = oDetalleVentaBono.MontoSobrePrecioSpecified = oDetalleVentaBono.PrecioListaSpecified =
                            oDetalleVentaBono.SKUSpecified = oDetalleVentaBono.tipoPromocionSpecified = true;
                    }
                    else
                    {
                        lstDettBonoNSTAux.Add(LstProdBonosID[i]);
                    }
                }
                LstProdBonosID = lstDettBonoNSTAux.ToArray();
            }

            if (lstDettBonoAux.Count > 0)
                lstDetalleVentaBono = lstDettBonoAux.ToArray();
            else
                lstDetalleVentaBono = new DetalleVentaBono[0];

            return lstDetalleVentaBono;
        }

        private void ActualizaProductosBono(DetalleVentaRes[] lstDetalleVentaRes, List<EntDetalleVentaResNST> lstEntDetalleVentaResNST)
        {
            //bool siAplica = false;
            for (int i = 0; i < lstDetalleVentaRes.Length; i++)
            {
                for (int j = 0; j < lstDetalleVentaRes[i].lstDetallesBono.Length; j++)
                {
                    EntDetalleVentaResNST oEntDetalleVentaResNST = new EntDetalleVentaResNST();
                    oEntDetalleVentaResNST.Cantidad = 1;
                    //this.ObtenerDatosProducto(ref lstDetalleVentaRes[i].lstDetallesBono[j]);
                    oEntDetalleVentaResNST.eTipoAgregadoNST = EnumTipoAgregadoNST.PromocionBono;
                    oEntDetalleVentaResNST.IdBonoAgregado = lstDetalleVentaRes[i].lstDetallesBono[j].IdBonoAgregado;
                    oEntDetalleVentaResNST.PrecioSugerido = lstDetalleVentaRes[i].lstDetallesBono[j].PrecioLista;
                    oEntDetalleVentaResNST.precioLista = lstDetalleVentaRes[i].lstDetallesBono[j].PrecioLista;
                    oEntDetalleVentaResNST.lstEntServicioSeleccionadoNST = new List<EntServicioSeleccionadoNST>();
                    oEntDetalleVentaResNST.lstMileniasDisponibles = new List<EntServicioNST>();
                    oEntDetalleVentaResNST.eTipoProductoNST = EnumTipoProductoNST.mercancias;
                    oEntDetalleVentaResNST.montoSobreprecio = lstDetalleVentaRes[i].lstDetallesBono[j].MontoSobrePrecio;
                    oEntDetalleVentaResNST.SKU = lstDetalleVentaRes[i].lstDetallesBono[j].SKU;
                    lstEntDetalleVentaResNST.Add(oEntDetalleVentaResNST);
                }
            }
        }

        private decimal ActualizaTotalBono(List<EntDetalleVentaResNST> lstEntDetalleVentaResNST)
        {
            decimal TotalBono = 0;
            for (int i = 0; i < lstEntDetalleVentaResNST.Count; i++)
                for (int j = 0; j < lstEntDetalleVentaResNST[i].lstEntPromocionAplicadaNST.Count; j++)
                    if (lstEntDetalleVentaResNST[i].lstEntPromocionAplicadaNST[j].eTipoPromocion == EnumTipoPromocionNST.Elektrapesos)
                    {
                        TotalBono += lstEntDetalleVentaResNST[i].lstEntPromocionAplicadaNST[j].montoOtorgado;
                        break;
                    }

            return TotalBono;
        }
        #endregion

        #region PrivadoSeguro
        private void CrearDetalleSeguroVidaMax(List<EntDetalleVentaBaseNST> lstEntDetalleVentaBaseNST, int plazo, bool esCalcularPrecio)
        {
            for (int i = 0; i < lstEntDetalleVentaBaseNST.Count; i++)
            {
                if (lstEntDetalleVentaBaseNST[i].eTipoProductoNST == EnumTipoProductoNST.seguroVida)
                {
                    if (this.oEntSeguroIpad.SKU == 0)
                    {
                        this.oEntSeguroIpad.SKU = lstEntDetalleVentaBaseNST[i].SKU;
                        this.oEntSeguroIpad.CostoSemanalSeguro = Convert.ToDouble(lstEntDetalleVentaBaseNST[i].precioLista);
                        this.oEntSeguroIpad.EsSeleccionado = true;
                        this.oEntSeguroIpad.IdVenta = 1;
                        this.oEntSeguroIpad.NumeroPlazo = plazo;
                        this.oEntSeguroIpad.Precio = lstEntDetalleVentaBaseNST[i].precioLista;
                        if (esCalcularPrecio)
                            this.oEntSeguroIpad.Precio = lstEntDetalleVentaBaseNST[i].precioLista * plazo;
                        this.oEntSeguroIpad.SumaAsegurada = Convert.ToDouble(lstEntDetalleVentaBaseNST[i].precioLista);
                        this.oEntSeguroIpad.Descripcion = lstEntDetalleVentaBaseNST[i].descripcion.Trim();

                        this.oEntSeguroIpad.CostoSemanalSeguroSpecified = this.oEntSeguroIpad.EsSeleccionadoSpecified = this.oEntSeguroIpad.IdVentaSpecified =
                            this.oEntSeguroIpad.NumeroPlazoSpecified = this.oEntSeguroIpad.PrecioSpecified = this.oEntSeguroIpad.SkuOtorgaSpecified =
                            this.oEntSeguroIpad.SKUSpecified = this.oEntSeguroIpad.SumaAseguradaSpecified = true;

                        this.oEntSeguroIpad.lstAtributos = new Atributo[4];
                        this.oEntSeguroIpad.lstAtributos[0] = new Atributo();
                        this.oEntSeguroIpad.lstAtributos[0].Key = "AbonoSeguro";
                        this.oEntSeguroIpad.lstAtributos[0].Value = lstEntDetalleVentaBaseNST[i].abonoProducto.ToString();
                        this.oEntSeguroIpad.lstAtributos[1] = new Atributo();
                        this.oEntSeguroIpad.lstAtributos[1].Key = "UltAbonoSeguro";
                        this.oEntSeguroIpad.lstAtributos[1].Value = lstEntDetalleVentaBaseNST[i].ultabonoProducto.ToString();
                        this.oEntSeguroIpad.lstAtributos[2] = new Atributo();
                        this.oEntSeguroIpad.lstAtributos[2].Key = "AbonoPPSeguro";
                        this.oEntSeguroIpad.lstAtributos[2].Value = lstEntDetalleVentaBaseNST[i].abonoPPProducto.ToString();
                        this.oEntSeguroIpad.lstAtributos[3] = new Atributo();
                        this.oEntSeguroIpad.lstAtributos[3].Key = "PrecioDeCredito";
                        this.oEntSeguroIpad.lstAtributos[3].Value = lstEntDetalleVentaBaseNST[i].precioCredito.ToString();

                        lstEntDetalleVentaBaseNST.Remove(lstEntDetalleVentaBaseNST[i]);
                        i--;
                    }
                    else
                    {
                        lstEntDetalleVentaBaseNST.Remove(lstEntDetalleVentaBaseNST[i]);
                        i--;
                    }
                }
            }
        }

        private EntDetalleVentaResNST CrearRespuestaDetalleSeguroVida()
        {
            return this.CrearRespuestaDetalleSeguroVida(null);
        }

        private EntDetalleVentaResNST CrearRespuestaDetalleSeguroVida(Atributo[] lstAtributosSeguro)
        {
            EntDetalleVentaResNST oEntDetalleVentaResNST = new EntDetalleVentaResNST();
            oEntDetalleVentaResNST.Cantidad = 1;
            oEntDetalleVentaResNST.descripcion = this.oEntSeguroIpad.Descripcion.Trim();
            oEntDetalleVentaResNST.precioLista = this.oEntSeguroIpad.Precio;
            oEntDetalleVentaResNST.SKU = this.oEntSeguroIpad.SKU;
            oEntDetalleVentaResNST.eTipoProductoNST = EnumTipoProductoNST.seguroVida;

            decimal UltAbono = 0, Abono = 0, AbonoPP = 0;
            int SKUSeguroAtributo = 0;

            if (lstAtributosSeguro != null && lstAtributosSeguro.Length > 0)
            {
                foreach (Atributo atributo in lstAtributosSeguro)
                {
                    switch (atributo.Key)
                    {
                        case "SKUSeguro":
                            SKUSeguroAtributo = Convert.ToInt32(atributo.Value);
                            break;
                        case "AbonoSeguro":
                            Abono = Convert.ToDecimal(atributo.Value);
                            break;
                        case "UltAbonoSeguro":
                            UltAbono = Convert.ToDecimal(atributo.Value);
                            break;
                        case "AbonoPPSeguro":
                            AbonoPP = Convert.ToDecimal(atributo.Value);
                            break;
                    }
                }
            }

            if (SKUSeguroAtributo == oEntDetalleVentaResNST.SKU)
            {
                oEntDetalleVentaResNST.abonoProducto = Abono;
                oEntDetalleVentaResNST.abonoPPProducto = AbonoPP;
                oEntDetalleVentaResNST.ultabonoProducto = UltAbono;
            }
            return oEntDetalleVentaResNST;
        }
        #endregion

        #region PrepagT
        public EntProductos ObtenerProductoPorUPC(string upc)
        {
            EntProductos prod = new EntProductos();
            prod.ObtenerProductoPorUPC(upc);
            return prod;
        }
        public EntRespuestaT ActivacionTarjetas(int pedido, string numeroTarjeta, string upc, string empleado, string password, string WS, int tipoOpetacion)
        {
            TrustAllCert();
            return new CtrlMediadorPrepago().OperacionesPrepago(pedido, numeroTarjeta, upc, empleado, password, WS, tipoOpetacion, ObtenerIPServidor());
        }

        //Metodo para burlar certificado
        public void TrustAllCert()
        {
            System.Net.ServicePointManager.ServerCertificateValidationCallback =
            ((remitente, certificado, cadena, sslPolicyErrors) => true);
        }

        //Metodo para buscar la ip del servidor
        public string ObtenerIPServidor()
        {
            string IP_SERVER = "";
            IPHostEntry heserver = Dns.GetHostEntry(Dns.GetHostName());
            if (heserver != null)
            {
                if (heserver.AddressList != null && heserver.AddressList.Length > 0)
                {
                    foreach (IPAddress ip in heserver.AddressList)
                        IP_SERVER = ip.ToString();
                }
            }

            return IP_SERVER;
        }
        #endregion

        #region Metodos pagare credito

        public EntResponseReimCreditoPagare ConsultaPedidoPagareCredito(int p_Pedido)
        {
            EntResponseReimCreditoPagare respuesta = new EntResponseReimCreditoPagare();

            try
            {
                System.Diagnostics.Trace.WriteLine("Se ejecuta el metodo ConsultaPedidoPagareCredito()", "LOG");

                respuesta = ObtenPedidoPagareCredito(p_Pedido);
                if (respuesta.CodigoError != 0)
                {
                    EntResponseReimCreditoPagare error = new EntResponseReimCreditoPagare();
                    error.CodigoError = respuesta.CodigoError;
                    error.Mensaje = respuesta.Mensaje;
                    error.DetalleTecnico = respuesta.DetalleTecnico;

                    return error;
                }
            }
            catch (Exception ex)
            {
                respuesta.CodigoError = 701;
                respuesta.Mensaje = "No se pudó realizar la consulta del pedido.";
                respuesta.DetalleTecnico = ex.Message;

                System.Diagnostics.Trace.WriteLine("stacktrace:" + ex.StackTrace, "LOG");
                System.Diagnostics.Trace.WriteLine("Detalles del error:" + respuesta.DetalleTecnico, "LOG");
                System.Diagnostics.Trace.WriteLine("mensaje:" + respuesta.Mensaje, "LOG");
            }

            return respuesta;
        }

        private EntResponseReimCreditoPagare ObtenPedidoPagareCredito(int p_Pedido)
        {
            EntResponseReimCreditoPagare respuesta = new EntResponseReimCreditoPagare();
            DataSet dSet = new DataSet();

            try
            {
                System.Diagnostics.Trace.WriteLine("Se ejecuta el metodo ConsultaPedidoPagareCredito() de la entidad", "LOG");
                Elektra.Services.DataAccess.AdoHelper AsistenteSql = Elektra.Services.DataAccess.AdoHelper.CreateHelper("Elektra.Services.DataAccess", "Elektra.Services.DataAccess.SqlServer");
                System.Collections.Hashtable Configuracion = (System.Collections.Hashtable)Microsoft.ApplicationBlocks.ConfigurationManagement.ConfigurationManager.Read("BaseDatosTienda");
                string CadenaConexion = (string)Configuracion["Elektra.cadenaDeConexion"];

                SqlParameter[] lstSqlParameter = new SqlParameter[1];
                lstSqlParameter[0] = new SqlParameter("@piPedido", SqlDbType.Int);
                lstSqlParameter[0].Value = p_Pedido;
                dSet = AsistenteSql.ExecuteDataset(CadenaConexion, "spconVtaCreditoPagare", lstSqlParameter);

                if (dSet != null && dSet.Tables != null && dSet.Tables.Count > 0 && dSet.Tables[0].Rows.Count > 0)
                {
                    DataRow datosComunes = dSet.Tables[0].Rows[0];

                    if (!(datosComunes["fiNoPedido"] is DBNull))
                        respuesta.Pedido = Convert.ToInt32(datosComunes["fiNoPedido"]);
                    if (!(datosComunes["fiNoTransac"] is DBNull))
                        respuesta.NoTrasaccion = Convert.ToInt32(datosComunes["fiNoTransac"]);
                    if (!(datosComunes["fiTipoVenta"] is DBNull))
                        respuesta.TipoVenta = Convert.ToByte(datosComunes["fiTipoVenta"]);
                    if (!(datosComunes["fcDescVenta"] is DBNull))
                        respuesta.DescripcionVenta = datosComunes["fcDescVenta"].ToString().Trim();
                    if (!(datosComunes["fdPedFec"] is DBNull))
                        respuesta.FechaPedido = FechaACamelCase(Convert.ToDateTime(datosComunes["fdPedFec"]));
                    if (!(datosComunes["fnPedTotal"] is DBNull))
                        respuesta.PedTotal = Convert.ToDecimal(datosComunes["fnPedTotal"]);
                    if (!(datosComunes["fnPagado"] is DBNull))
                        respuesta.Pagado = Convert.ToDecimal(datosComunes["fnPagado"]);

                    respuesta.ClienteDatos = new EntReimClienteDatos();
                    if (!(datosComunes["fcCteNombre"] is DBNull))
                        respuesta.ClienteDatos.Nombre = datosComunes["fcCteNombre"].ToString().Trim();
                    if (!(datosComunes["fcCteApaterno"] is DBNull))
                        respuesta.ClienteDatos.ApellidoPaterno = datosComunes["fcCteApaterno"].ToString().Trim();
                    if (!(datosComunes["fcCteAMaterno"] is DBNull))
                        respuesta.ClienteDatos.ApellidoMaterno = datosComunes["fcCteAMaterno"].ToString().Trim();
                    if (!(datosComunes["fiPaisCteU"] is DBNull))
                        respuesta.ClienteDatos.Pais = Convert.ToByte(datosComunes["fiPaisCteU"]);
                    if (!(datosComunes["fiCanalCteU"] is DBNull))
                        respuesta.ClienteDatos.Canal = Convert.ToByte(datosComunes["fiCanalCteU"]);
                    if (!(datosComunes["fiSucursalCteU"] is DBNull))
                        respuesta.ClienteDatos.Sucursal = Convert.ToInt32(datosComunes["fiSucursalCteU"]);
                    if (!(datosComunes["fiFolioCteU"] is DBNull))
                        respuesta.ClienteDatos.Folio = Convert.ToInt32(datosComunes["fiFolioCteU"]);
                    if (!(datosComunes["fiNgcioId"] is DBNull))
                        respuesta.ClienteDatos.NegocioId = Convert.ToByte(datosComunes["fiNgcioId"]);
                    if (!(datosComunes["fiNoTienda"] is DBNull))
                        respuesta.ClienteDatos.NoTienda = Convert.ToInt16(datosComunes["fiNoTienda"]);
                    if (!(datosComunes["fiCteId"] is DBNull))
                        respuesta.ClienteDatos.CteId = Convert.ToInt32(datosComunes["fiCteId"]);
                    if (!(datosComunes["fiDigitoVer"] is DBNull))
                        respuesta.ClienteDatos.DigitoVer = Convert.ToByte(datosComunes["fiDigitoVer"]);

                    respuesta.PedidoDetalle = new List<EntReimpPedidoDetalle>();

                    foreach (DataRow filaDetallePresupuesto in dSet.Tables[0].Rows)
                    {
                        EntReimpPedidoDetalle producto = new EntReimpPedidoDetalle();

                        if (!(filaDetallePresupuesto["fiProdId"] is DBNull))
                            producto.ProductoId = Convert.ToInt32(filaDetallePresupuesto["fiProdId"]);
                        if (!(filaDetallePresupuesto["fcproddesc"] is DBNull))
                            producto.Descripcion = filaDetallePresupuesto["fcproddesc"].ToString().Trim();
                        if (!(filaDetallePresupuesto["fnPrcUnit"] is DBNull))
                            producto.ProdPrecio = Convert.ToDecimal(filaDetallePresupuesto["fnPrcUnit"]);
                        if (!(filaDetallePresupuesto["fnPdctoDescto"] is DBNull))
                            producto.Descuento = Convert.ToDecimal(filaDetallePresupuesto["fnPdctoDescto"]);
                        if (!(filaDetallePresupuesto["fnProdDesctoE"] is DBNull))
                            producto.DescuentoEsp = Convert.ToDecimal(filaDetallePresupuesto["fnProdDesctoE"]);
                        if (!(filaDetallePresupuesto["fiCantArt"] is DBNull))
                            producto.Cantidad = Convert.ToInt32(filaDetallePresupuesto["fiCantArt"]);
                        if (!(filaDetallePresupuesto["fnTotal"] is DBNull))
                            producto.Total = Convert.ToDecimal(filaDetallePresupuesto["fnTotal"]);

                        respuesta.PedidoDetalle.Add(producto);
                    }
                }

            }
            catch (Exception ex)
            {
                if (ex.GetType() == typeof(SqlException))
                {
                    if (((SqlException)ex).Number == 50000)
                    {
                        respuesta.CodigoError = 700;
                        respuesta.Mensaje = ex.Message;
                        respuesta.DetalleTecnico = string.Empty;
                    }
                }

                if (respuesta.CodigoError == 0)
                {
                    respuesta.CodigoError = 702;
                    respuesta.Mensaje = "Ocurrió un error con la consulta de la información.";
                    respuesta.DetalleTecnico = ex.Message;
                }

                System.Diagnostics.Trace.WriteLine("stacktrace:" + ex.StackTrace, "LOG");
                System.Diagnostics.Trace.WriteLine("Detalles del error:" + respuesta.DetalleTecnico, "LOG");
                System.Diagnostics.Trace.WriteLine("mensaje:" + respuesta.Mensaje, "LOG");
            }

            return respuesta;
        }

        [System.Diagnostics.DebuggerStepThrough]
        protected string FechaACamelCase(DateTime p_Fecha)
        {
            System.Globalization.CultureInfo cultureInfo = System.Threading.Thread.CurrentThread.CurrentCulture;
            System.Globalization.TextInfo textInfo = cultureInfo.TextInfo;
            return textInfo.ToTitleCase(p_Fecha.ToString("dddd", System.Globalization.CultureInfo.CreateSpecificCulture("es-ES"))) + p_Fecha.ToString(", dd \\de ") + textInfo.ToTitleCase(p_Fecha.ToString("MMMM", System.Globalization.CultureInfo.CreateSpecificCulture("es-ES"))) + p_Fecha.ToString(" \\de yyyy");
        }

        #endregion Metodos pagare credito

        #region Consultas acceso

        public string ConsultaUrlComoGano(string p_Empleado)
        {
            string respuesta = string.Empty;

            try
            {
                System.Diagnostics.Trace.WriteLine("Se ejecuta el metodo ConsultaUrlComoGano()", "LOG");
                Elektra.ComoGano.PortalSIE.Login acceso = new Elektra.ComoGano.PortalSIE.Login();
                respuesta = acceso.PortalSIE(p_Empleado);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine("stacktrace:" + ex.StackTrace, "LOG");
                System.Diagnostics.Trace.WriteLine("Detalles del error:" + ex.Message, "LOG");
                respuesta = string.Format("Error: {0}", ex.Message);
            }

            return respuesta;
        }

        #endregion Consultas acceso

        #region Recompensas al recomendar

        public EntCaracteristicaRespuesta CaracteristicasClienteRecomendado(EntCaracteristicaPeticion caracteristica)
        {
            var metodo = this.GetType().Name + "." + System.Reflection.MethodBase.GetCurrentMethod().Name;
            System.Diagnostics.Trace.WriteLine("Inicia " + metodo, "LOG");
            var respuesta = new EntCaracteristicaRespuesta();
            try
            {
                string url = new EntCatalogos().ObtenerParametroNegocio(186);
                respuesta = new WSRecompensasRecomendar() { Url = url }.CaracteristicasClienteRecomendado(caracteristica);
                respuesta.data.montoMinimoCompra = String.IsNullOrEmpty(respuesta.data.montoMinimoCompra) ? "0" : respuesta.data.montoMinimoCompra;
                respuesta.data.montoPremio = String.IsNullOrEmpty(respuesta.data.montoPremio) ? "0" : respuesta.data.montoPremio;
                respuesta.data.descuentoCada = String.IsNullOrEmpty(respuesta.data.descuentoCada) ? "0" : respuesta.data.descuentoCada;
            }
            catch (Exception)
            {
                respuesta.error = true;
            }
            System.Diagnostics.Trace.WriteLine("Fin de " + metodo, "LOG");
            return respuesta;
        }

        public EntDescuentoRespuesta ValidaDescuentoClienteRecomendado(EntDescuentoPeticion descuento)
        {
            var metodo = this.GetType().Name + "." + System.Reflection.MethodBase.GetCurrentMethod().Name;
            System.Diagnostics.Trace.WriteLine("Inicia " + metodo, "LOG");
            var respuesta = new EntDescuentoRespuesta();
            try
            {
                string url = new EntCatalogos().ObtenerParametroNegocio(186);
                respuesta = new WSRecompensasRecomendar() { Url = url }.ValidaDescuentoClienteRecomendado(descuento);
            }
            catch (Exception)
            {
                respuesta.error = true;
            }
            System.Diagnostics.Trace.WriteLine("Fin de " + metodo, "LOG");
            return respuesta;
        }

        #endregion

        #region Up Selling y Cross Selling

        public EntRespuestaProductosMejora ObtenerProductosMejora(int sku, int pais, int canal, int sucursal, int tipoVenta, int periodo, decimal capacidadCredito)
        {
            var metodo = this.GetType().Name + "." + System.Reflection.MethodBase.GetCurrentMethod().Name;
            Trace.WriteLine("Inicio de: " + metodo, "LOG");
            Trace.WriteLine(string.Format("{0} --> Parámetros: sku: {1}, pais: {2}, canal: {3}, sucursal: {4}, tipoVenta: {5}, periodo {6}, capacidadCredito {7}", metodo, sku, pais, canal, sucursal, tipoVenta, periodo, capacidadCredito), "LOG");
            EntRespuestaProductosMejora respuesta = new EntRespuestaProductosMejora();

            try
            {
                bool aplicarPromocion = false;
                var entConsultasBD = new EntConsultasBDNST();
                List<EntProductoMejora> productosMejora = new List<EntProductoMejora>();
                DataSet dsProductosMejora = entConsultasBD.ConsultaProductosMejora(sku);
                
                //Valida si existen productos en BD.
                if (dsProductosMejora != null && dsProductosMejora.Tables != null && dsProductosMejora.Tables.Count > 0 && dsProductosMejora.Tables[0].Rows.Count > 0)
                {
                    Trace.WriteLine(metodo + " --> Obtuvo lista productos desde BD.", "LOG");
                    DataSet dsProductoBueno = entConsultasBD.ConsultaDescuentosProducto(sku);
                    EntProductoMejora productoBuenoBD = new EntProductoMejora();
                    
                    //Valida si existen carácteristicas de producto Bueno en BD.
                    if (dsProductoBueno != null && dsProductoBueno.Tables != null && dsProductoBueno.Tables.Count > 0 && dsProductoBueno.Tables[0].Rows.Count > 0)
                    {
                        Trace.WriteLine(metodo + " --> Obtuvo características de producto bueno desde BD.", "LOG");
                        DataRow drProductoBueno = dsProductoBueno.Tables[0].Rows[0];
                        productoBuenoBD = new EntProductoMejora()
                        {
                            Caracteristicas = drProductoBueno["fcCaract"].ToString(),
                            Categoria = (EnumTipoProductoNST)Enum.ToObject(typeof(EnumTipoProductoNST), Convert.ToInt32(drProductoBueno["fiTipoProducto"])),
                            Descripcion = drProductoBueno["fcProdDesc"].ToString(),
                            Descuento = Convert.ToDecimal(drProductoBueno["fnDescuento"]),
                            Marca = drProductoBueno["fcMarca"].ToString(),
                            Modelo = drProductoBueno["fcModelo"].ToString(),
                            Precio = Convert.ToDecimal(drProductoBueno["fnProdPrecio"]),
                            PrecioDescuento = Convert.ToDecimal(drProductoBueno["fnPrecioDescuento"]),
                            Seleccionado = false,
                            Sku = Convert.ToInt32(drProductoBueno["fiProdId"]),
                            TAG = Enum.GetName(typeof(EntEnumTipoProducto), EntEnumTipoProducto.Bueno),
                            Tipo = EntEnumTipoProducto.Bueno
                        };

                        productosMejora.Add(productoBuenoBD);

                        foreach (DataRow drProducto in dsProductosMejora.Tables[0].Rows)
                        {
                            EntProductoMejora productoMejora = new EntProductoMejora()
                            {
                                Caracteristicas = drProducto["fcCaract"].ToString(),
                                Categoria = (EnumTipoProductoNST)Enum.ToObject(typeof(EnumTipoProductoNST), Convert.ToInt32(drProducto["fiTipoProducto"])),
                                Descripcion = drProducto["fcProdDesc"].ToString(),
                                Descuento = Convert.ToDecimal(drProducto["fnDescuento"]),
                                Marca = drProductoBueno["fcMarca"].ToString(),
                                Modelo = drProductoBueno["fcModelo"].ToString(),
                                Orden = Convert.ToInt32(drProducto["fiTipoOrdenID"]),
                                Precio = Convert.ToDecimal(drProducto["fnProdPrecio"]),
                                PrecioDescuento = Convert.ToDecimal(drProducto["fnPrecioDescuento"]),
                                Prioridad = Convert.ToInt32(drProducto["fiPrioridad"]),
                                Seleccionado = false,
                                Sku = Convert.ToInt32(drProducto["fiSkuPromocion"]),
                                TAG = drProducto["fcDescripcion"].ToString(),
                                Tipo = (EntEnumTipoProducto)Enum.ToObject(typeof(EntEnumTipoProducto), Convert.ToInt32(drProducto["fiTipoAdicionalId"]))
                            };
                            productosMejora.Add(productoMejora);
                        }

                        Trace.WriteLine(metodo + " --> Productos desde BD: " + JsonConvert.SerializeObject(productosMejora), "LOG");

                        //Tipo de venta a crédito, consulta los plazos en la API.
                        if (tipoVenta == 2)
                        {
                            Trace.WriteLine(metodo + " --> Es venta a crédito.", "LOG");
                            string tipoCliente = tipoPromocion = null;

                            if (productoBuenoBD.Categoria == EnumTipoProductoNST.mercancias)
                            {
                                tipoCliente = "56";
                                tipoPromocion = "21";
                            }

                            if (productoBuenoBD.Categoria == EnumTipoProductoNST.motos)
                            {
                                tipoCliente = "56";
                                tipoPromocion = "36";
                            }

                            if (productoBuenoBD.Categoria == EnumTipoProductoNST.telefonia)
                            {
                                tipoCliente = "3";
                                tipoPromocion = "0";
                            }

                            EntCarritoRequestVenta peticionPlazosApi = new EntCarritoRequestVenta()
                            {
                                tienda = sucursal,
                                canal = canal,
                                pais = pais,
                                tipoCliente = tipoCliente,
                                tipoPromocion = tipoPromocion,
                                tipoProducto = null,
                                tipoEtiquetado = 0,
                                tipoPeriodo = periodo
                            };

                            //Llenado objeto con los productos a consultar en la API.
                            foreach (var producto in productosMejora)
                            {
                                EntDetalleVentaBaseNST entDetalleVentaBaseNST = new EntDetalleVentaBaseNST()
                                {
                                    SKU = producto.Sku,
                                    precioLista = producto.Precio,
                                    montoDescuento = producto.Descuento,
                                    montoEnganche = 0,
                                    plazo = 0,
                                    Cantidad = 1
                                };

                                peticionPlazosApi.lstEntDetalleVentaBaseNST.Add(entDetalleVentaBaseNST);
                            }

                            Trace.WriteLine(metodo + " --> Petición a API de Plazos: " + JsonConvert.SerializeObject(peticionPlazosApi), "LOG");

                            EntPlazosVenta respuestaPlazosApi = new ManejadorAPISCredito().APICotizarVentaCredito(peticionPlazosApi);

                            Trace.WriteLine(metodo + " --> Respuesta de API de Plazos: " + JsonConvert.SerializeObject(respuestaPlazosApi), "LOG");

                            //Error en API, retorna excepción a la respuesta del servicio.
                            if (respuestaPlazosApi.oEntRespuestaNST.eTipoError > 0) throw new Exception("Error al consultar los plazos desde la API.");

                            //Asigna el plazo máximo para los productos devueltos desde la API.
                            foreach (var producto in productosMejora)
                            {
                                EntPlazosSKU plazos = respuestaPlazosApi.LstPlazosSKU.FirstOrDefault(plazo => plazo.SKU == producto.Sku);
                                if (plazos != null)
                                {
                                    EntPlazoAbono plazoMaximo = plazos.lstEntPlazoAbono.OrderByDescending(plazo => plazo.abono).First();
                                    producto.Plazo = plazoMaximo;
                                }
                            }

                        }

                        //Obtiene el producto BUENO.
                        EntProductoMejora productoBueno = null;
                        productoBueno = productosMejora
                            .Where(p => p.Tipo == EntEnumTipoProducto.Bueno)
                            .FirstOrDefault();

                        //Obtiene el producto OPTIMO de acuerdo a las reglas de negocio.
                        EntProductoMejora productoOptimo = null;
                        if (tipoVenta == 2)
                        {
                            productoOptimo = productosMejora
                                .Where(p => p.Tipo == EntEnumTipoProducto.Optimo && p.Plazo.abono <= capacidadCredito)
                                .OrderBy(p => p.Prioridad)
                                .FirstOrDefault();
                        }
                        else
                        {
                            productoOptimo = productosMejora
                                .Where(p => p.Tipo == EntEnumTipoProducto.Optimo)
                                .OrderBy(p => p.Prioridad)
                                .FirstOrDefault();
                        }

                        //Obtiene el producto IDEAL de acuerdo a las reglas de negocio.
                        EntProductoMejora productoIdeal = null;
                        if (tipoVenta == 2)
                        {
                            productoIdeal = productosMejora
                                .Where(p => p.Tipo == EntEnumTipoProducto.Ideal && p.Plazo.abono <= capacidadCredito)
                                .OrderBy(p => p.Prioridad)
                                .FirstOrDefault();
                        }
                        else
                        {
                            productoIdeal = productosMejora
                                .Where(p => p.Tipo == EntEnumTipoProducto.Ideal)
                                .OrderBy(p => p.Prioridad)
                                .FirstOrDefault();
                        }

                        Trace.WriteLine(metodo + " --> Producto seleccionado como BUENO: " + JsonConvert.SerializeObject(productoBueno), "LOG");
                        Trace.WriteLine(metodo + " --> Producto seleccionado como OPTIMO: " + JsonConvert.SerializeObject(productoOptimo), "LOG");
                        Trace.WriteLine(metodo + " --> Producto seleccionado como IDEAL: " + JsonConvert.SerializeObject(productoIdeal), "LOG");

                        aplicarPromocion = (productoBueno != null) && (productoOptimo != null || productoIdeal != null);
                        Trace.WriteLine(metodo + " --> Aplicar promoción: " + Convert.ToString(aplicarPromocion), "LOG");

                        respuesta.ProductosMejora = !aplicarPromocion ? null : new List<EntProductoMejora>() {
                            productoBueno,
                            productoOptimo,
                            productoIdeal
                        };
                        Trace.WriteLine(metodo + " --> Respuesta Servicio: " + JsonConvert.SerializeObject(respuesta), "LOG");

                        //var aplicarPromocion1 = productosMejora.Where(p => p.Tipo == EntEnumTipoProducto.Bueno).Any() && productosMejora.Where(p => p.Tipo == EntEnumTipoProducto.Optimo || p.Tipo == EntEnumTipoProducto.Ideal).Any();
                        


                    }


                }

            }
            catch (Exception ex)
            {
                respuesta.EsError = true;
                respuesta.MensajeTecnico = ex.Message;
                respuesta.MensajeUsuario = "Ocurrió un error al obtener los productos de mejora.";
                Trace.WriteLine(metodo + " --> Ocurrió una excepción, mensaje: " + ex.Message, "LOG");
            }

            Trace.WriteLine("Fin de: " + metodo, "LOG");
            return respuesta;
        }

        public EntRespuestaProductosMejora ObtenerProductosVentaCruzada(int sku, int pais, int canal, int sucursal, int tipoVenta, int periodo, decimal capacidadCredito)
        {
            var metodo = this.GetType().Name + "." + System.Reflection.MethodBase.GetCurrentMethod().Name;
            Trace.WriteLine("Inicio de: " + metodo, "LOG");
            Trace.WriteLine(string.Format("{0} --> Parámetros: sku: {1}, pais: {2}, canal: {3}, sucursal: {4}, tipoVenta: {5}, periodo {6}, capacidadCredito {7}", metodo, sku, pais, canal, sucursal, tipoVenta, periodo, capacidadCredito), "LOG");
            EntRespuestaProductosMejora respuesta = new EntRespuestaProductosMejora();

            try
            {
                bool aplicarPromocion = false;
                var entConsultasBD = new EntConsultasBDNST();
                List<EntProductoMejora> productosMejora = new List<EntProductoMejora>();
                DataSet dsProductosMejora = entConsultasBD.ConsultaProductosMejora(sku);

                //Valida si existen productos en BD.
                if (dsProductosMejora != null && dsProductosMejora.Tables != null && dsProductosMejora.Tables.Count > 0 && dsProductosMejora.Tables[0].Rows.Count > 0)
                {
                    Trace.WriteLine(metodo + " --> Obtuvo lista productos desde BD.", "LOG");
                    DataSet dsProductoBueno = entConsultasBD.ConsultaDescuentosProducto(sku);
                    EntProductoMejora productoBuenoBD = new EntProductoMejora();

                    //Valida si existen carácteristicas de producto Bueno en BD.
                    if (dsProductoBueno != null && dsProductoBueno.Tables != null && dsProductoBueno.Tables.Count > 0 && dsProductoBueno.Tables[0].Rows.Count > 0)
                    {
                        Trace.WriteLine(metodo + " --> Obtuvo características de producto bueno desde BD.", "LOG");
                        DataRow drProductoBueno = dsProductoBueno.Tables[0].Rows[0];
                        productoBuenoBD = new EntProductoMejora()
                        {
                            Caracteristicas = drProductoBueno["fcCaract"].ToString(),
                            Categoria = (EnumTipoProductoNST)Enum.ToObject(typeof(EnumTipoProductoNST), Convert.ToInt32(drProductoBueno["fiTipoProducto"])),
                            Descripcion = drProductoBueno["fcProdDesc"].ToString(),
                            Descuento = Convert.ToDecimal(drProductoBueno["fnDescuento"]),
                            Precio = Convert.ToDecimal(drProductoBueno["fnProdPrecio"]),
                            PrecioDescuento = Convert.ToDecimal(drProductoBueno["fnPrecioDescuento"]),
                            Seleccionado = false,
                            Sku = Convert.ToInt32(drProductoBueno["fiProdId"]),
                            Tipo = EntEnumTipoProducto.Bueno
                        };

                        productosMejora.Add(productoBuenoBD);

                        foreach (DataRow drProducto in dsProductosMejora.Tables[0].Rows)
                        {
                            EntProductoMejora productoMejora = new EntProductoMejora()
                            {
                                Caracteristicas = drProducto["fcCaract"].ToString(),
                                Categoria = (EnumTipoProductoNST)Enum.ToObject(typeof(EnumTipoProductoNST), Convert.ToInt32(drProducto["fiTipoProducto"])),
                                Descripcion = drProducto["fcProdDesc"].ToString(),
                                Descuento = Convert.ToDecimal(drProducto["fnDescuento"]),
                                Orden = Convert.ToInt32(drProducto["fiTipoOrdenID"]),
                                Precio = Convert.ToDecimal(drProducto["fnProdPrecio"]),
                                PrecioDescuento = Convert.ToDecimal(drProducto["fnPrecioDescuento"]),
                                Prioridad = Convert.ToInt32(drProducto["fiPrioridad"]),
                                Seleccionado = false,
                                Sku = Convert.ToInt32(drProducto["fiSkuPromocion"]),
                                Tipo = (EntEnumTipoProducto)Enum.ToObject(typeof(EntEnumTipoProducto), Convert.ToInt32(drProducto["fiTipoAdicionalId"]))
                            };
                            productosMejora.Add(productoMejora);
                        }

                        Trace.WriteLine(metodo + " --> Productos desde BD: " + JsonConvert.SerializeObject(productosMejora), "LOG");

                        //Tipo de venta a crédito, consulta los plazos en la API.
                        if (tipoVenta == 2)
                        {
                            Trace.WriteLine(metodo + " --> Es venta a crédito.", "LOG");
                            string tipoCliente = tipoPromocion = null;

                            if (productoBuenoBD.Categoria == EnumTipoProductoNST.mercancias)
                            {
                                tipoCliente = "56";
                                tipoPromocion = "21";
                            }

                            if (productoBuenoBD.Categoria == EnumTipoProductoNST.motos)
                            {
                                tipoCliente = "56";
                                tipoPromocion = "36";
                            }

                            if (productoBuenoBD.Categoria == EnumTipoProductoNST.telefonia)
                            {
                                tipoCliente = "3";
                                tipoPromocion = "0";
                            }

                            EntCarritoRequestVenta peticionPlazosApi = new EntCarritoRequestVenta()
                            {
                                tienda = sucursal,
                                canal = canal,
                                pais = pais,
                                tipoCliente = tipoCliente,
                                tipoPromocion = tipoPromocion,
                                tipoProducto = null,
                                tipoEtiquetado = 0,
                                tipoPeriodo = periodo
                            };

                            //Llenado objeto con los productos a consultar en la API.
                            foreach (var producto in productosMejora)
                            {
                                EntDetalleVentaBaseNST entDetalleVentaBaseNST = new EntDetalleVentaBaseNST()
                                {
                                    SKU = producto.Sku,
                                    precioLista = producto.Precio,
                                    montoDescuento = producto.Descuento,
                                    montoEnganche = 0,
                                    plazo = 0,
                                    Cantidad = 1
                                };

                                peticionPlazosApi.lstEntDetalleVentaBaseNST.Add(entDetalleVentaBaseNST);
                            }

                            Trace.WriteLine(metodo + " --> Petición a API de Plazos: " + JsonConvert.SerializeObject(peticionPlazosApi), "LOG");

                            EntPlazosVenta respuestaPlazosApi = new ManejadorAPISCredito().APICotizarVentaCredito(peticionPlazosApi);

                            Trace.WriteLine(metodo + " --> Respuesta de API de Plazos: " + JsonConvert.SerializeObject(respuestaPlazosApi), "LOG");

                            //Error en API, retorna excepción a la respuesta del servicio.
                            if (respuestaPlazosApi.oEntRespuestaNST.eTipoError > 0) throw new Exception("Error al consultar los plazos desde la API.");

                            //Asigna el plazo máximo para los productos devueltos desde la API.
                            foreach (var producto in productosMejora)
                            {
                                EntPlazosSKU plazos = respuestaPlazosApi.LstPlazosSKU.FirstOrDefault(plazo => plazo.SKU == producto.Sku);
                                if (plazos != null)
                                {
                                    EntPlazoAbono plazoMaximo = plazos.lstEntPlazoAbono.OrderByDescending(plazo => plazo.abono).First();
                                    producto.Plazo = plazoMaximo;
                                }
                            }

                        }

                        //Obtiene el producto BUENO.
                        EntProductoMejora productoBueno = null;
                        productoBueno = productosMejora
                            .Where(p => p.Tipo == EntEnumTipoProducto.Bueno)
                            .FirstOrDefault();

                        //Obtiene el producto COMPLEMENTARIO 1 de acuerdo a las reglas de negocio.
                        EntProductoMejora productoComplementario1 = null;
                        if (tipoVenta == 2)
                        {
                            productoComplementario1 = productosMejora
                                .Where(p => p.Tipo == EntEnumTipoProducto.Complementario1 && p.Plazo.abono <= capacidadCredito)
                                .OrderBy(p => p.Prioridad)
                                .FirstOrDefault();
                        }
                        else
                        {
                            productoComplementario1 = productosMejora
                                .Where(p => p.Tipo == EntEnumTipoProducto.Complementario1)
                                .OrderBy(p => p.Prioridad)
                                .FirstOrDefault();
                        }

                        //Obtiene el producto COMPLEMENTARIO 2 de acuerdo a las reglas de negocio.
                        EntProductoMejora productoComplementario2 = null;
                        if (tipoVenta == 2)
                        {
                            productoComplementario2 = productosMejora
                                .Where(p => p.Tipo == EntEnumTipoProducto.Complementario2 && p.Plazo.abono <= capacidadCredito)
                                .OrderBy(p => p.Prioridad)
                                .FirstOrDefault();
                        }
                        else
                        {
                            productoComplementario2 = productosMejora
                                .Where(p => p.Tipo == EntEnumTipoProducto.Complementario2)
                                .OrderBy(p => p.Prioridad)
                                .FirstOrDefault();
                        }

                        //Obtiene los productos de ACCESORIOS de acuerdo a las reglas de negocio.
                        List<EntProductoMejora> productosAccesorios = null;
                        if (tipoVenta == 2)
                        {
                            productosAccesorios = productosMejora
                                .Where(p => p.Tipo == EntEnumTipoProducto.Accesorio && p.Plazo.abono <= capacidadCredito)
                                .OrderBy(p => p.Prioridad)
                                .ToList();
                        }
                        else
                        {
                            productosAccesorios = productosMejora
                                .Where(p => p.Tipo == EntEnumTipoProducto.Accesorio)
                                .OrderBy(p => p.Prioridad)
                                .ToList();
                        }


                        Trace.WriteLine(metodo + " --> Producto seleccionado como COMPLEMENTARIO 1: " + JsonConvert.SerializeObject(productoComplementario1), "LOG");
                        Trace.WriteLine(metodo + " --> Producto seleccionado como COMPLEMENTARIO 2: " + JsonConvert.SerializeObject(productoComplementario2), "LOG");
                        //Trace.WriteLine(metodo + " --> Producto seleccionado como IDEAL: " + JsonConvert.SerializeObject(productoIdeal), "LOG");

                        //aplicarPromocion = (productoBueno != null) && (productoOptimo != null || productoIdeal != null);
                        //Trace.WriteLine(metodo + " --> Aplicar promoción: " + Convert.ToString(aplicarPromocion), "LOG");

                        //respuesta.ProductosMejora = !aplicarPromocion ? null : new List<EntProductoMejora>() {
                        //    productoBueno,
                        //    productoOptimo,
                        //    productoIdeal
                        //};

                        respuesta.ProductosMejora = productosAccesorios;

                        Trace.WriteLine(metodo + " --> Respuesta Servicio: " + JsonConvert.SerializeObject(respuesta), "LOG");
                    }
                }
            }
            catch (Exception ex)
            {
                respuesta.EsError = true;
                respuesta.MensajeTecnico = ex.Message;
                respuesta.MensajeUsuario = "Ocurrió un error al obtener los productos de venta cruzada.";
                Trace.WriteLine(metodo + " --> Ocurrió una excepción, mensaje: " + ex.Message, "LOG");
            }

            Trace.WriteLine("Fin de: " + metodo, "LOG");
            return respuesta;
        }

        public EntRespuestaBase GuardarProductosMejora(int presupuesto, List<EntProductoMejora> productos)
        {
            //{
            //    "presupuesto": 100,
            //    "productos" : [
            //        {
            //            "Sku" : 1000
            //        }
            //    ]
            //}

            var metodo = this.GetType().Name + "." + System.Reflection.MethodBase.GetCurrentMethod().Name;
            Trace.WriteLine("Inicio de: " + metodo, "LOG");
            Trace.WriteLine(string.Format("{0} --> Parámetros: presupuesto: {1}, productos: {2}", metodo, presupuesto, JsonConvert.SerializeObject(productos)), "LOG");
            EntRespuestaBase respuesta = new EntRespuestaBase();

            try
            {
                var entConsultasBD = new EntConsultasBDNST();
                foreach (var producto in productos)
                {
                    entConsultasBD.GuardarRegistroVenta(presupuesto, (int)producto.Tipo, producto.Sku, producto.Plazo.plazo, producto.Precio, producto.PrecioDescuento, producto.Plazo.abonoPuntual);
                }
            }
            catch (Exception ex)
            {
                respuesta.EsError = true;
                respuesta.MensajeTecnico = ex.Message;
                respuesta.MensajeUsuario = "Ocurrió un error al guardar los productos de mejora.";
                Trace.WriteLine(metodo + " --> Ocurrió una excepción, mensaje: " + ex.Message, "LOG");
            }

            Trace.WriteLine("Fin de: " + metodo, "LOG");
            return respuesta;
        }

        public string ObtenerProductosMejora2(int sku, int pais, int canal, int sucursal, int tipoVenta, int periodo, decimal capacidadCredito)//(int sku, int plazo, int pais, int canal, int tienda)
        {
            //$scope.TipoPlanCredito -> periodo
            string test = string.Empty;
            try
            {
                EntProductoMejora productoBueno = new EntProductoMejora();
                List<EntProductoMejora> productosMejora = new List<EntProductoMejora>();
                var entConsultasBD = new EntConsultasBDNST();
                //ConsultaCaracteristicasProducto


                DataSet dsProductosMejora = entConsultasBD.ConsultaProductosMejora(sku);
                if (dsProductosMejora != null && dsProductosMejora.Tables != null && dsProductosMejora.Tables.Count > 0 && dsProductosMejora.Tables[0].Rows.Count > 0) {
                    
                    DataSet dsProductoBueno = entConsultasBD.ConsultaDescuentosProducto(sku);
                    if (dsProductoBueno != null && dsProductoBueno.Tables != null && dsProductoBueno.Tables.Count > 0 && dsProductoBueno.Tables[0].Rows.Count > 0) {
                        DataRow drProductoBueno = dsProductoBueno.Tables[0].Rows[0];
                        productoBueno = new EntProductoMejora() {
                            Caracteristicas = drProductoBueno["fcCaract"].ToString(),
                            Categoria = (EnumTipoProductoNST)Enum.ToObject(typeof(EnumTipoProductoNST), Convert.ToInt32(drProductoBueno["fiTipoProducto"])),
                            Descripcion = drProductoBueno["fcProdDesc"].ToString(),
                            Descuento = Convert.ToDecimal(drProductoBueno["fnDescuento"]),
                            Precio = Convert.ToDecimal(drProductoBueno["fnProdPrecio"]),
                            PrecioDescuento = Convert.ToDecimal(drProductoBueno["fnPrecioDescuento"]),
                            Seleccionado = false,
                            Sku = Convert.ToInt32(drProductoBueno["fiProdId"]),
                            Tipo = EntEnumTipoProducto.Bueno
                        };
                        productosMejora.Add(productoBueno);
                    }

                    foreach (DataRow drProducto in dsProductosMejora.Tables[0].Rows)
                    {
                        EntProductoMejora productoMejora = new EntProductoMejora()
                        {
                            Caracteristicas = drProducto["fcCaract"].ToString(),
                            Categoria = (EnumTipoProductoNST)Enum.ToObject(typeof(EnumTipoProductoNST), Convert.ToInt32(drProducto["fiTipoProducto"])),
                            Descripcion = drProducto["fcProdDesc"].ToString(),
                            Descuento = Convert.ToDecimal(drProducto["fnDescuento"]),
                            Orden = Convert.ToInt32(drProducto["fiTipoOrdenID"]),
                            Precio = Convert.ToDecimal(drProducto["fnProdPrecio"]),
                            PrecioDescuento = Convert.ToDecimal(drProducto["fnPrecioDescuento"]),
                            Prioridad = Convert.ToInt32(drProducto["fiPrioridad"]),
                            Seleccionado = false,
                            Sku = Convert.ToInt32(drProducto["fiSkuPromocion"]),
                            Tipo = (EntEnumTipoProducto)Enum.ToObject(typeof(EntEnumTipoProducto), Convert.ToInt32(drProducto["fiTipoAdicionalId"]))
                        };
                        productosMejora.Add(productoMejora);
                    }
                }

                if (tipoVenta == 2) //Crédito
                {
                    string tipoCliente = tipoPromocion = null;
                    if (productoBueno.Categoria == EnumTipoProductoNST.mercancias){
                        tipoCliente = "56";
                        tipoPromocion = "21";
                    }
                    if (productoBueno.Categoria == EnumTipoProductoNST.motos){
                        tipoCliente = "56";
                        tipoPromocion = "36";
                    }
                    if (productoBueno.Categoria == EnumTipoProductoNST.telefonia){
                        tipoCliente = "3";
                        tipoPromocion = "0";
                    }
                    EntCarritoRequestVenta peticionPlazosApi = new EntCarritoRequestVenta(){
                        tienda = sucursal,
                        canal = canal,
                        pais = pais,
                        tipoCliente = tipoCliente,
                        tipoPromocion = tipoPromocion,
                        tipoProducto = null,
                        tipoEtiquetado = 0,
                        tipoPeriodo = periodo
                    };

                    //Asigna los productos a consultar en API.
                    foreach (var producto in productosMejora)
                    {
                        EntDetalleVentaBaseNST entDetalleVentaBaseNST = new EntDetalleVentaBaseNST(){
                            SKU = producto.Sku,
                            precioLista = producto.Precio,
                            montoDescuento = producto.Descuento,
                            montoEnganche = 0,
                            plazo = 0,
                            Cantidad = 1
                        };
                        peticionPlazosApi.lstEntDetalleVentaBaseNST.Add(entDetalleVentaBaseNST);
                    }

                    EntPlazosVenta respuestaPlazosApi = new ManejadorAPISCredito().APICotizarVentaCredito(peticionPlazosApi);

                    
                    //Error en API.
                    if (respuestaPlazosApi.oEntRespuestaNST.eTipoError > 0)
                    {
                        throw new Exception("Error al consultar los plazos desde API.");
                    }

                    
                    //Asigna los plazos devueltos desde API.
                    foreach (var producto in productosMejora)
                    {
                        EntPlazosSKU plazos = respuestaPlazosApi.LstPlazosSKU.FirstOrDefault(plazo => plazo.SKU == producto.Sku);
                        if (plazos != null)
                        {
                            EntPlazoAbono plazoMaximo = plazos.lstEntPlazoAbono.OrderByDescending(plazo => plazo.abono).First();
                            producto.Plazo = plazoMaximo;
                        }
                    }

                    //Asigna el producto bueno de acuerdo a las reglas de negocio.
                    EntProductoMejora _productoBueno = productosMejora
                        .Where(p => p.Tipo == EntEnumTipoProducto.Bueno && p.Plazo.abono <= capacidadCredito)
                        .OrderByDescending(p => p.Prioridad)
                        .ThenByDescending(p => p.Plazo.abono)
                        .FirstOrDefault();

                    //Asigna el producto optimo de acuerdo a las reglas de negocio.
                    //EntProductoMejora productoOptimo = productosMejora
                    //    .Where(p => p.Tipo == EntEnumTipoProducto.Optimo && p.Plazo.abono <= capacidadCredito)
                    //    .OrderByDescending(p => p.Prioridad)
                    //    .ThenByDescending(p => p.Plazo.abono)
                    //    .FirstOrDefault();


                    //Asigna el producto ideal de acuerdo a las reglas de negocio.
                    EntProductoMejora productoIdeal = productosMejora
                        .Where(p => p.Tipo == EntEnumTipoProducto.Ideal && p.Plazo.abono <= capacidadCredito)
                        .OrderByDescending(p => p.Prioridad)
                        .ThenByDescending(p => p.Plazo.abono)
                        .FirstOrDefault();

                    //Asigna el producto complementario 1 de acuerdo a las reglas de negocio.
                    EntProductoMejora productoComplemento1 = productosMejora
                        .Where(p => p.Tipo == EntEnumTipoProducto.Complementario1 && p.Plazo.abono <= capacidadCredito)
                        .OrderByDescending(p => p.Prioridad)
                        .ThenByDescending(p => p.Plazo.abono)
                        .FirstOrDefault();

                    //Asigna el producto complementario 2 de acuerdo a las reglas de negocio.
                    EntProductoMejora productoComplemento2 = productosMejora
                        .Where(p => p.Tipo == EntEnumTipoProducto.Complementario2 && p.Plazo.abono <= capacidadCredito)
                        .OrderByDescending(p => p.Prioridad)
                        .ThenByDescending(p => p.Plazo.abono)
                        .FirstOrDefault();

                    test = JsonConvert.SerializeObject(respuestaPlazosApi);
                }

                EntProductoMejora productoOptimo = tipoVenta == 2 ? productosMejora
                .Where(p => p.Tipo == EntEnumTipoProducto.Optimo && p.Plazo.abono <= capacidadCredito)
                .OrderBy(p => p.Plazo.abono)
                .OrderByDescending(p => p.Prioridad)
                .FirstOrDefault() : productosMejora
                .Where(p => p.Tipo == EntEnumTipoProducto.Optimo)
                .OrderBy(p => p.Prioridad)
                .FirstOrDefault();

                test = JsonConvert.SerializeObject(productoOptimo);
            }
            catch (Exception)
            {
                
                throw;
            }
            return test;
        }



        #endregion

        private string ObjectToXML(Object objectClass)
        {
            XmlDocument xmlDoc = new XmlDocument();
            XmlSerializer xmlSerializer = new XmlSerializer(objectClass.GetType());
            using (MemoryStream xmlStream = new MemoryStream())
            {
                xmlSerializer.Serialize(xmlStream, objectClass);
                xmlStream.Position = 0;
                xmlDoc.Load(xmlStream);
                return xmlDoc.InnerXml;
            }
        }
        public int ValidaSiTienePaquete(int sku)
        {
            return new ManejadorCombosTotalPlay().ValidaTienePaquetes(sku);
        }

    }
}
