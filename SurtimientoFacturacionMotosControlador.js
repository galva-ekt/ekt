menuHead.controller('facturacionMotosController', ['$scope', 'servicioFacturacionMotos', 'ngDialog', '$sce', '$q','mensajeServicio','$timeout','MenuPrincipalCabeceroFabrica','surtimientoServicios','$location','servicioFacturacionContado','Navegacion','$interval',
  function($scope, servicioFacturacionMotos, ngDialog, $sce, $q, mensajeServicio,$timeout,MenuPrincipalCabeceroFabrica,surtimientoServicios,$location, servicioFacturacionContado, Navegacion,$interval) {
		$scope.botonBloqueo = false;
		$scope.empNoMH = MenuPrincipalCabeceroFabrica.getEmpNoMH();
		$scope.estacionMH = MenuPrincipalCabeceroFabrica.getEstacionMH();
		$scope.ipServidorMH = MenuPrincipalCabeceroFabrica.getIpServidorMH();
	

	$scope.validarNITFactura = function() {
		var frmNIT = $scope.cliente.RFC;
		var idDnit = mensajeServicio.indicadorEspera('Validando N.I.T.', 'rojo');
		var frmNombre = $scopecliente.Nombre;
		
		if(frmNIT == "CF" && frmNombre == ""){
				mensajeServicio.mensajeError({titulo:'Error', msjUsuario: "Para el guardado de C.F. debe ingresar el nombre del Cliente.", clase: 'rojo'});
				return;
		}
		var parametrosNit = { pedido: $scope.DatosPedido.idPedido, nit: frmNIT, nombre: frmNombre, direccion: "", esMoto: false};
		servicioFacturacionContado.validaNIT(parametrosNit).then(
			function(response) {
				mensajeServicio.cerrarIndicadorEsperaId(idDnit);
				if(response.data.EsError)
					mensajeServicio.mensajeError({titulo:'Error', msjUsuario: response.data.MensajeUsuario, msjTecnico: response.data.MensajeTecnico, clase: 'rojo'});
				else{
					$scope.nitValido = true;
					if(frmNIT != "CF") $scope.cliente.Nombre = response.data.NombreCliente;
				}
			},
			function(error)
			{
				mensajeServicio.cerrarIndicadorEsperaId(idDnit);
				mensajeServicio.mensajeError({titulo: 'Error', msjUsuario: "Error al validar N.I.T.", msjTecnico : error.status + " - " + error.statusText, clase: 'rojo'});
			}
		);
	};

	$scope.consultaDatosFacturacionITK = function() {
      if($scope.esMotosNuevasMarcas){
		  $scope.datosFacturacionNuevasMarcas();
	  }else{
		  $scope.datosFacturacionITK();
	  }
    };
	
	$scope.datosFacturacionITK = function(){
		servicioFacturacionMotos.conEstatusFacturacion({pedido: $scope.DatosPedido.idPedido, origen: 2}).then(
        function(response) {
          if (response.data.Estatus === 4) {
            /*NO FACTURADA*/
            $scope.conClienteEKT();
            $scope.conItalikaEKT();
          } else if (response.data.Estatus === 3) {
            /*FACTURADO*/
            // $scope.impresiones();
          } else {
            /*error*/
            mensajeServicio.mensajeError({titulo: 'Atenci\u00f3n!', msjUsuario:response.data.Descripcion, msjTecnico:response.data.MsgCatch, clase: 'rojo'});
          }
        }, function(error) {
          mensajeServicio.mensajeError({titulo: 'Error', msjUsuario: "Error al consultar los datos de facturaci\u00f3n", msjTecnico : error.status + " - " + error.statusText, clase: 'rojo'});
        }
      );
	}
	
	$scope.datosFacturacionNuevasMarcas = function(){
		servicioFacturacionMotos.conDatosMNM({pedido: $scope.DatosPedido.idPedido}).then(
		function(response) {
			$scope.italika = response.data;
			$scope.ajusteFormMoto();
		}, function(error) {
			mensajeServicio.mensajeError({titulo: 'Error', msjUsuario: "Error al consultar las características de la motocicleta.", msjTecnico : error.status + " - " + error.statusText, clase: 'rojo'});
		});
		
		servicioFacturacionMotos.conClienteMNM({pedido: $scope.DatosPedido.idPedido}).then(
		function(response) {
			$scope.cliente = response.data;
			$scope.ajusteFormCliente();
		}, function(error) {
			mensajeServicio.mensajeError({titulo: 'Error', msjUsuario: "Error al consultar los datos del cliente.", msjTecnico : error.status + " - " + error.statusText, clase: 'rojo'});
		});
	}
	
    $scope.conClienteITK = function() {
      servicioFacturacionMotos.conClienteITK({pedido: $scope.DatosPedido.idPedido}).then(
        function(response) {
          if (response.data.EsError) {
            $scope.conClienteEKT();
          } else
            $scope.cliente = response.data;
			$scope.ajusteFormCliente();
        }, function(error) {
          mensajeServicio.mensajeError({titulo: 'Error', msjUsuario: "Error al consultar los datos de cliente ITK", msjTecnico : error.status + " - " + error.statusText, clase: 'rojo'});
        });
	};
	
    $scope.conClienteEKT = function() {
      servicioFacturacionMotos.conClienteEKT({pedido: $scope.DatosPedido.idPedido}).then(
        function(response) {
            $scope.cliente = response.data;
			$scope.ajusteFormCliente();
        }, function(error) {
          mensajeServicio.mensajeError({titulo: 'Error', msjUsuario: "Error al consultar los datos de cliente EKT", msjTecnico : error.status + " - " + error.statusText, clase: 'rojo'});
        });
    };
	
	$scope.ajusteFormCliente = function(){
		$scope.bloqueaDatosCliente = false;
		
		if($scope.esConSeguroDanos && $scope.esConSeguroVidaMax && $scope.seguroDatosIguales){
			$scope.cliente.Nombre = $scope.datosClienteSeguroMostrador[0].Nombre;
			$scope.cliente.ApPaterno = $scope.datosClienteSeguroMostrador[0].ApellidoPaterno;
			$scope.cliente.ApMaterno = $scope.datosClienteSeguroMostrador[0].ApellidoMaterno;
			$scope.cliente.Telefono = $scope.datosClienteSeguroMostrador[0].Telefono;
			$scope.cliente.RFC = $scope.datosClienteSeguroMostrador[0].RFC;
			$scope.cliente.Estado = $scope.datosClienteSeguroMostrador[0].NombreEntidad;
			$scope.cliente.Calle = $scope.datosClienteSeguroMostrador[0].Calle;
			$scope.cliente.NoExterior = $scope.datosClienteSeguroMostrador[0].NumeroExterior;
			$scope.cliente.NoInterior = $scope.datosClienteSeguroMostrador[0].NumeroInterior;
			$scope.cliente.Colonia = $scope.datosClienteSeguroMostrador[0].Colonia;
			$scope.cliente.CP = $scope.datosClienteSeguroMostrador[0].CodigoPostal;
			$scope.bloqueaDatosCliente = true;
		}else if($scope.esConSeguroDanos){
			var ind = 0;
			
			for(var x = 0; x<$scope.datosClienteSeguroMostrador.length;x++){
				if( $scope.datosClienteSeguroMostrador[x].tipo === 2 )
					ind = x;
			}
				
			$scope.cliente.Nombre = $scope.datosClienteSeguroMostrador[ind].Nombre;
			$scope.cliente.ApPaterno = $scope.datosClienteSeguroMostrador[ind].ApellidoPaterno;
			$scope.cliente.ApMaterno = $scope.datosClienteSeguroMostrador[ind].ApellidoMaterno;
			$scope.cliente.Telefono = $scope.datosClienteSeguroMostrador[ind].Telefono;
			$scope.cliente.RFC = $scope.datosClienteSeguroMostrador[ind].RFC;
			$scope.cliente.Estado = $scope.datosClienteSeguroMostrador[ind].NombreEntidad;
			$scope.cliente.Calle = $scope.datosClienteSeguroMostrador[ind].Calle;
			$scope.cliente.NoExterior = $scope.datosClienteSeguroMostrador[ind].NumeroExterior;
			$scope.cliente.NoInterior = $scope.datosClienteSeguroMostrador[ind].NumeroInterior;
			$scope.cliente.Colonia = $scope.datosClienteSeguroMostrador[ind].Colonia;
			$scope.cliente.CP = $scope.datosClienteSeguroMostrador[ind].CodigoPostal;
			$scope.bloqueaDatosCliente = true;
		}else{
		for(var prop in $scope.cliente){
				if(typeof $scope.cliente[prop] === 'string' || typeof $scope.cliente[prop] === 'number')
				{
					if(prop != 'Ciudad' && prop != 'Delegacion' && prop != 'RazonSocial')
						{
							if($scope.DatosPedido.TipoVenta === 4 && prop!='NoPedido')
							{
							 $scope.cliente[prop] = '';
							}else
							{
							 var cad = String($scope.cliente[prop]);
							 $scope.cliente[prop] = cad.trim();
							 }
						}
				}
			}
			$scope.cliente.Nombre = $scope.cliente.Nombre.replace(/\s\s+/g, ' ');
			if(($scope.cliente.ApPaterno == null || $scope.cliente.ApPaterno == "") && ($scope.cliente.ApMaterno == null || $scope.cliente.ApMaterno == "")){
				var arN = $scope.cliente.Nombre.split(" ");
				$scope.cliente.ApPaterno = arN[arN.length - 2 ];
				$scope.cliente.ApMaterno = arN[arN.length - 1 ];
				$scope.cliente.Nombre = "";
				for(var x = 0; x< (arN.length - 2); x++)
				{
					$scope.cliente.Nombre += arN[x] + " ";
				}
			}
		}
	};
	
    $scope.conItalikaITK = function() {
      servicioFacturacionMotos.conItalikaITK({pedido: $scope.DatosPedido.idPedido}).then(
        function(response) {
          if (response.data.EsError) {
            $scope.conItalikaEKT();
			$scope.ajusteFormMoto();
          } else
            $scope.italika = response.data;
        }, function(error) {
          mensajeServicio.mensajeError({titulo: 'Error', msjUsuario: "Error al consultar los datos de italika ITK", msjTecnico : error.status + " - " + error.statusText, clase: 'rojo'});
      });
	};
	
    $scope.conItalikaEKT = function() {
      servicioFacturacionMotos.conItalikaEKT({pedido: $scope.DatosPedido.idPedido}).then(
        function(response) {
          $scope.italika = response.data;
		  $scope.ajusteFormMoto();
        }, function(error) {
          mensajeServicio.mensajeError({titulo: 'Error', msjUsuario: "Error al consultar los datos de italika EKT", msjTecnico : error.status + " - " + error.statusText, clase: 'rojo'});
      });
	};
	
	$scope.ajusteFormMoto = function(){
		for(var prop in $scope.italika){
				if(typeof $scope.italika[prop] === 'string' || typeof $scope.italika[prop] === 'number')
				{
					//if(prop != 'Ciudad' && prop != 'Delegacion' && prop != 'RazonSocial')
						//{
						 var cad = String($scope.italika[prop]);
						 $scope.italika[prop] = cad.trim();
						//}
				}
			}
	};
	
    $scope.ConExistBiometrico = function() {
      servicioFacturacionMotos.ConExistBiometrico({pedido: $scope.DatosPedido.idPedido}).then(
        function(response) {
          console.log(response);
        }, function(error) {
          mensajeServicio.mensajeError({titulo: 'Error', msjUsuario: "Error al consultar datos biometrico", msjTecnico : error.status + " - " + error.statusText, clase: 'rojo'});
      });
    };
	
	$scope.facturarMotocicleta = function(){
		$scope.datosClienteFacturaMoto.$setSubmitted();
		if ($scope.datosClienteFacturaMoto.$valid)
        {
			if($scope.esMotosNuevasMarcas){
				$scope.generaFacturaMostrador();
			}else{
				$scope.aceptoTerminos();
			}
		}
	}
	
	$scope.aceptoTerminos = function() {
		$scope.botonBloqueo = true;
		var i = 0;
		$scope.strBio = '30820201308201AB048201723082016E3034302F0201030201020410';
		while( i < 1141)
		{
			var r = Math.random();
			$scope.strBio += (r<0.1?Math.floor(r*100):String.fromCharCode(Math.floor(r*26) + (r>0.5?97:65)).toUpperCase());
			i++;
		}
		
		if($scope.DatosPedido.TipoVenta === 2) {
			$scope.botonBloqueo = false;
			$('head').append('<link rel="stylesheet" type="text/css" href="/Elektrafront/epos/MarcadoVenta/css/eposTools.css" id="surt-css-huella">');
			eposTools.run({
				id: null,
				objCliente: null,
				objVenta: { plazo: 0, abono: null, total: 0},
				lectorHuella:(typeof CJSGlobalObject !== 'undefined' ? CJSGlobalObject : null),
				callback : $scope.callBackHuella,
				dummy: { titulo: 'AVISO IMPORTANTE'},
				esUareU: typeof esUareU !== 'undefined' ? esUareU : false,
				empleado: typeof UserID !== 'undefined' ? UserID : null
			});
		}else{
			$scope.generaFacturaMostrador();
		}
	}
	
	$scope.callBackHuella = function(respValidacionHuella)
	{
		$('head #surt-css-huella').remove();
		if(respValidacionHuella)
			$scope.guardaDatosFacturacion();
	};

    $scope.guardaDatosFacturacion = function() {
		if($scope.esMotosNuevasMarcas){
			$scope.impresionDocsMotosNueva();
		}
		else{
			var idD = mensajeServicio.indicadorEspera('Guardando datos del cliente', 'rojo');		
			servicioFacturacionMotos.insItalika($scope.italika).then(
				function(resItalika) {
					if (!resItalika.data.EsError) {
						servicioFacturacionMotos.insTerminos({pedido: $scope.DatosPedido.idPedido, acepta: 1, biometrico: $scope.strBio}).then(
							function(respTerm) {
								mensajeServicio.cerrarIndicadorEsperaId(idD);
								if (!respTerm.data.EsError) {

									if ($scope.DatosPedido.EsImpresionNueva) {
											$scope.impresionDocsMotosNueva();
									}
									else {
										console.log('Impresion vieja');
										$scope.impresionDocsMotos();
									}
								}else{
									mensajeServicio.mensajeError({titulo: 'Error', msjUsuario: respTerm.data.MsgUsr, msjTecnico: respTerm.data.MsgCatch , clase: 'rojo'});
								}
							}, 	
							function(error) {
								mensajeServicio.cerrarIndicadorEsperaId(idD);
								mensajeServicio.mensajeError({titulo: 'Error', msjUsuario: "Error al guardar terminos: " + error.status + " - " + error.statusText , clase: 'rojo'});
							}
						);
					}else{
						mensajeServicio.cerrarIndicadorEsperaId(idD);
						mensajeServicio.mensajeError({titulo: 'Error', msjUsuario: resItalika.data.MsgUsr, msjTecnico: resItalika.data.MsgCatch , clase: 'rojo'});
					}
				}, 
				function(error) {
					mensajeServicio.cerrarIndicadorEsperaId(idD);
					mensajeServicio.mensajeError({titulo: 'Error', msjUsuario: "Error al guardar datos de la italika: " + error.status + " - " + error.statusText , clase: 'rojo'});
				}
			);
		}
    };
	
	$scope.generaFacturaMostrador = function(){
		
		var idDnit = mensajeServicio.indicadorEspera('Guardando N.I.T.', 'rojo');
		var parametrosNit = { pedido: $scope.DatosPedido.idPedido, nit: $scope.cliente.RFC, nombre: $scope.cliente.Nombre, esMoto: true};
		servicioFacturacionContado.validaNIT(parametrosNit).then(
			function(response) {
				mensajeServicio.cerrarIndicadorEsperaId(idDnit);
				if(response.data.EsError)
					mensajeServicio.mensajeError({titulo:'Error', msjUsuario: response.data.MensajeUsuario, msjTecnico: response.data.MensajeTecnico, clase: 'rojo'});
			},
			function(error)
			{
				mensajeServicio.cerrarIndicadorEsperaId(idDnit);
				mensajeServicio.mensajeError({titulo: 'Error', msjUsuario: "Ocurrió un error al guardar el N.I.T.", msjTecnico : error.status + " - " + error.statusText, clase: 'rojo'});
			}
		);
		
		var idF = mensajeServicio.indicadorEspera('Solicitando Factura', 'rojo');
		$scope.FacturacionEPOS = { oEntPeticionFacturaNST : { esDesgloceIVA : false, idPedido : $scope.DatosPedido.idPedido,
			oEntDatosEntradaNST : { eTipoLlamadoNST : 0, idSesion : 0, idUsuario: mensajeServicio.numEmpSinT( MenuPrincipalCabeceroFabrica.getEmpNoMH() ), ws: MenuPrincipalCabeceroFabrica.getEstacionMH()},
			oEntClienteFacturaNST : { RFCCliente: $scope.cliente.RFC, nombreCompleto : $scope.cliente.Nombre}}
		};
				
		servicioFacturacionContado.facturacionEPOS($scope.FacturacionEPOS).then(
			function(response) {
				mensajeServicio.cerrarIndicadorEsperaId(idF);
				$scope.botonBloqueo = false;
				if(response.data.FacturacionEPOSResult.oEntRespuestaNST.eTipoError !== 0){
					mensajeServicio.mensajeError({titulo:'Error al generar factura', msjUsuario: response.data.FacturacionEPOSResult.oEntRespuestaNST.mensajeError, clase: 'rojo'});
				}
				else
				{
					$scope.listaFacturasMostrador = response.data.FacturacionEPOSResult.lstFacturas;
					$scope.guardaDatosFacturacion();
				}
			},
			function(error)
			{
				$scope.botonBloqueo = false;
				mensajeServicio.cerrarIndicadorEsperaId(idF);
				mensajeServicio.mensajeError({titulo: 'Error', msjUsuario: "Error al generar factura: ", msjTecnico : error.status + " - " + error.statusText, clase: 'rojo'});
			}
		);
	};

    /*Impresion de las nuevas cartas*/
		$scope.imprimirCartasMotos= function (params,noEmpleado,estacionTrabajo){

			surtimientoServicios.cadenasImpresionCartaNuevoMotos(params,$scope.ipServidorMH,noEmpleado,estacionTrabajo).then(
function (response) {

	console.log('Cadenas Nueva impresion');
	console.log(response);
	console.log(MenuPrincipalCabeceroFabrica.getIpEstacionMH());
	if (response.data.ListaDocumentosCartaXSLT.length > 0/*!response.data.error*/) {
			console.log('Respondio bien');
			console.log(response.data.datCartas);
			surtimientoServicios.generarCartaImpresion(response.data.ListaDocumentosCartaXSLT, MenuPrincipalCabeceroFabrica.getIpEstacionMH()).then(
					function (responseImp) {
							if (responseImp.data.EstatusExito) {
									surtimientoServicios.mandarCartaImprimir(responseImp.data.Contenido, MenuPrincipalCabeceroFabrica.getIpEstacionMH()).then(
											function (response) {

													if (!responseImp.data.EstatusExito) {
															alert('Error al mandar los tickets a impresion' + ' ' + response.data.Detalle );
															
													} else {
														var banderaTicket = 0;

														lstDetalleVenta.every(function(item){if(item.eTipoProductoNST === "12CartaPolizaServicios"){banderaTicket = 1;return false;}else{return true;}});
														if(banderaTicket === 1){
															$scope.imprimirTicket(response.data.ListaTickets)
														}
													}
											},
											function (error) {
													alert('Error al mandar los tickets a impresion' + ' ' + error.status );
													
											});

							}
							else {
									alert('Error al generar los tickets de impresion' + ' ' + response.data.Detalle);
									
							}

					},
					function (error) {
							alert("Error al generar los tickets de impresion" + ' ' + error.status);
							
					});
	}
	else {
			alert("Error al crear los tickets de impresion" + ' ' + response.data.mensaje);
			
	}
},
function (error) {
	alert("Error al crear los tickets de impresion" + ' ' + error.status);
	
});

}

	/*Impresion de nuevas de factura contado*/
	$scope.imprimirCartasFactura= function (params){

		servicioFacturacionContado.imprimirFacturaNuevo(params).then(
function (response) {

console.log('Cadenas Nueva impresion');
console.log(response);
console.log(MenuPrincipalCabeceroFabrica.getIpEstacionMH());
if (response.data.detCartasFacturacion.length > 0/*!response.data.error*/) {
		console.log('Respondio bien');
		console.log(response.data.detCartasFacturacion);
		surtimientoServicios.generarCartaImpresion(response.data.detCartasFacturacion, MenuPrincipalCabeceroFabrica.getIpEstacionMH()).then(
				function (responseImp) {
						if (responseImp.data.EstatusExito) {
								surtimientoServicios.mandarCartaImprimir(responseImp.data.Contenido, MenuPrincipalCabeceroFabrica.getIpEstacionMH()).then(
										function (response) {
										
												if (!responseImp.data.EstatusExito) {
														alert('Error al mandar a mandar a imprimir cartas de facturacion' + ' ' + response.data.Detalle );
														
												} else {

												}
										},
										function (error) {
												alert('Error al mandar a imprimir cartas de facturacion' + ' ' + error.status );
												
										});

						}
						else {
								alert('Error al generar a imprimir cartas de facturacion' + ' ' + response.data.Detalle);
								
						}

				},
				function (error) {
						alert("Error al generar a imprimir cartas de facturacion" + ' ' + error.status);
						
				});
}
else {
		alert("Error al crear los tickets de impresion" + ' ' + response.data.mensaje);
		
}
},
function (error) {
alert("Error al crear los tickets de impresion" + ' ' + error.status);

});

}

    
	$scope.impresionDocsMotos = function() {
      var idI = mensajeServicio.indicadorEspera('Imprimiendo Documentos', 'rojo');
	  $scope.colaImpresion = { tiempoTotal: 0, impresiones : [] };
	  
	  /*factura contado**/
	  if($scope.DatosPedido.TipoVenta !== 2)
	  {
		var impresionFC = {fun: function(){
			var facturaImpresion = { xslFactura :  $scope.listaFacturasMostrador, idPedido: $scope.DatosPedido.idPedido, ipWS: MenuPrincipalCabeceroFabrica.getIpEstacionMH(), ws: MenuPrincipalCabeceroFabrica.getEstacionMH() };
								servicioFacturacionContado.imprimirFactura(facturaImpresion).then(function(respImpr){
									if(!respImpr.data.error)
									{
										$scope.idSessionImpresionFactura = respImpr.data.idSessionImpresion;
									}
									else{
										$scope.idSessionImpresionFactura = null;
										mensajeServicio.mensajeError({titulo: 'Error', msjUsuario: 'Ocurrio un error al imprimir la factura', msjTecnico: respImpr.data.mensaje,  clase: 'rojo'});
										}
								},
								function(error){
									mensajeServicio.mensajeError({titulo: 'Error', msjUsuario: "Error al imprimir la factura", msjTecnico : error.status + " - " + error.statusText, clase: 'rojo'});
								});	
			},
			tiempoEspera: $scope.colaImpresion.tiempoTotal + 25000};
			$scope.colaImpresion.tiempoTotal += 25000;				 
			$scope.colaImpresion.impresiones.push(impresionFC);
		}


		/*facturacion credito y docs motos*/
		var impresionDocs = {fun: function(){ $scope.urlImpresionesMoto = $sce.trustAsResourceUrl('/ElektraFront/ImpresionTicketSurtimientoPromociones/Promociones/ImpresionNotaEntrega.htm?tipoImpresion=4&pedido=' + $scope.DatosPedido.idPedido + '&empleado=' + mensajeServicio.numEmpSinT( MenuPrincipalCabeceroFabrica.getEmpNoMH() ) + '&ws=' + MenuPrincipalCabeceroFabrica.getEstacionMH() + '&ipWS=' + MenuPrincipalCabeceroFabrica.getIpEstacionMH()); },
						   tiempoEspera: $scope.colaImpresion.tiempoTotal + 35000};
		$scope.colaImpresion.tiempoTotal += 35000;				 
		$scope.colaImpresion.impresiones.push(impresionDocs);
	  
		for(var i = 0; i< $scope.colaImpresion.impresiones.length; i++)
		{
			if(i === 0){/*primer impresion, no requiere espera*/
				$scope.colaImpresion.impresiones[i].fun();
			}else{
				(function(i){  
					var imprInt = $interval(function(){
						$scope.colaImpresion.impresiones[i].fun();
						$interval.cancel(imprInt);
					}, $scope.colaImpresion.impresiones[i - 1].tiempoEspera);
				})(i); 
			}
		}
			
		$timeout(function() {
			mensajeServicio.cerrarIndicadorEsperaId(idI);
			$scope.confImpr = ngDialog.open({
			  template: 'Templates/SurtimientoImpresionConfirma.html',
			  className: 'ngdialog-theme-plain',
			  showClose: false,
			  closeByDocument: false,
			  closeByEscape: false,
			  scope: $scope,
			  cache: false
			});
			}, $scope.colaImpresion.tiempoTotal);
			
			
	  
		};


	$scope.impresionDocsMotosNueva = function() {
		var idI = mensajeServicio.indicadorEspera('Imprimiendo Documentos', 'rojo');
		$scope.colaImpresion = { tiempoTotal: 0, impresiones : [] };
		
		/*factura contado**/
		if($scope.DatosPedido.TipoVenta !== 2 || $scope.esMotosNuevasMarcas)
		{
			/*FACTURACION*/
			$scope.colaImpresion.tiempoTotal += 25000;
			var facturaImpresion = { xslFactura :  $scope.listaFacturasMostrador, idPedido: $scope.DatosPedido.idPedido, ipWS: MenuPrincipalCabeceroFabrica.getIpEstacionMH(), ws: MenuPrincipalCabeceroFabrica.getEstacionMH() };
			$scope.imprimirCartasFactura(facturaImpresion);
		}
		
		if(!$scope.esMotosNuevasMarcas){
			/*Se incorpora el nuevo componente de impresion de Italika*/
			$scope.imprimirCartasMotos($scope.DatosPedido.idPedido,	$scope.empNoMH,MenuPrincipalCabeceroFabrica.getEstacionMH());
		}
		
		$timeout(function() {
			mensajeServicio.cerrarIndicadorEsperaId(idI);
			$scope.confImpr = ngDialog.open({
				template: 'Templates/SurtimientoImpresionConfirma.html',
				className: 'ngdialog-theme-plain',
				showClose: false,
				closeByDocument: false,
				closeByEscape: false,
				scope: $scope,
				cache: false
			});
		},12000);
	};
	


    $scope.terminoImpresion = function(termino) {
		ngDialog.close($scope.confImpr.id);/*cierra el dialog de confirma*/
		if (termino)
			$scope.finImpresionMotos();
		else{
			$scope.urlImpresionesMoto = $sce.trustAsResourceUrl('about:blank');
			$scope.urlFinImprFactura = $sce.trustAsResourceUrl('about:blank');
			$timeout(function() {
				//$scope.impresionDocsMotos();
				if ($scope.DatosPedido.EsImpresionNueva) {
					console.log('Impresion nueva');
					$scope.impresionDocsMotosNueva();
				}
				else {
					console.log('Impresion vieja');
					$scope.impresionDocsMotos();
				}
				
			},1000);
		}        
    };

    $scope.finImpresionMotos = function() {
      var esp = mensajeServicio.indicadorEspera('Espere por favor', 'rojo');
      if (typeof idSessionImpresionDocsMotos !== "undefined") {
        $scope.urlImpresionesMoto = $sce.trustAsResourceUrl('/ElektraFront/ImpresionTicketSurtimientoPromociones/Promociones/ImpresionNotaEntrega.htm?tipoImpresion=6&idSession=' + idSessionImpresionDocsMotos + '&idDatos=MT' + $scope.DatosPedido.idPedido + '&modoImpresion=CTAXML');
        idSessionImpresionDocsMotos = undefined;
      }
	  if(typeof $scope.idSessionImpresionFactura !== "undefined" && $scope.idSessionImpresionFactura !== null ){
			$scope.urlFinImprFactura = $sce.trustAsResourceUrl('/ElektraFront/ImpresionTicketSurtimientoPromociones/Promociones/ImpresionNotaEntrega.htm?tipoImpresion=6&idSession=' + $scope.idSessionImpresionFactura + '&idDatos=FC' + $scope.DatosPedido.idPedido + '&modoImpresion=CTAXML');
			$scope.idSessionImpresionFactura = undefined;
	 }
      $timeout(function() {
        mensajeServicio.cerrarIndicadorEsperaId(esp);
		$scope.retornoNavegacion();
      }, 2000);
    };
	
	
	////
	// Funciones del Ticket
	////
	
	$scope.imprimirTicket = function(tickets){
    var ipEstacion = MenuPrincipalCabeceroFabrica.getIpEstacionMH();
    definirImpresora();
    
    //1. Definir Impresora Ticket
    function definirImpresora(){
        var mensajeError = "Ocurrió un error al definir la impresora de tickets en el servicio de impresión.";
        surtimientoServicios.registraTicketImpresion().then(function (respuesta) {
            if(respuesta.data.EstatusExito)
                guardarImpresion(tickets);
            else
                errorImpresion(mensajeError, respuesta.Detalle);
        }, function (error) {
            errorImpresion(mensajeError, error.status);
        });
    }
    
    //2. Generar Ticket
    function guardarImpresion(tickets){
        var mensajeError = "Ocurrió un error al guardar los tickets en el servicio de impresión.";
        surtimientoServicios.generarTicketImpresion(tickets, ipEstacion).then(function (respuesta) {
            if(respuesta.data.EstatusExito)
                imprimirContenido(respuesta.data.Contenido);
            else
                errorImpresion(mensajeError, respuesta.Detalle);
        }, function (error) {
            errorImpresion(mensajeError, error.status);
        });
    }
    
    //3. Imprimir Ticket
    function imprimirContenido(idContenido){
        var mensajeError = "Ocurrió un error al imprimir el contenido de los tickets en el servicio de impresión.";
        surtimientoServicios.mandarImprimir(idContenido, ipEstacion).then(function (respuesta) {
            if(!respuesta.data.EstatusExito)
                errorImpresion(mensajeError, respuesta.Detalle);
        }, function (error) {
            errorImpresion(mensajeError, error.status);
        });
    }
    
    //Método de Error
    function errorImpresion(mensajeUsuario, mensajeTecnico){
        alert(mensajeUsuario + ", Detalle: " + mensajeTecnico);
	}
	
	//Valida NIT
	$scope.validaNIT = function() {
		$scope.datosClienteFactura.$setSubmitted();
		
		if ($scope.datosClienteFactura.$valid)
		{

		var nit = mensajeServicio.indicadorEsperaId('Validando N.I.T.', 'rojo');
		servicioFacturacionContado.validaNIT($scope.validaNIT).then(
				function(response) {
					mensajeServicio.cerrarIndicadorEsperaId(nit);
					if(response.data.validaNIT.oEntRespuestaNST.eTipoError !== 0)
						mensajeServicio.mensajeError({titulo:'Error', msjUsuario: response.data.validaNIT.oEntRespuestaNST.mensajeError, clase: 'rojo'});
					else
						$scope.impresiones(response.data.validaNIT);
				},
				function(error)
				{
					mensajeServicio.cerrarIndicadorEsperaId(nit);
					mensajeServicio.mensajeError({titulo: 'Error', msjUsuario: "Error al validar NIT", msjTecnico : error.status + " - " + error.statusText, clase: 'rojo'});
				}
		);
		}
	};
};
	
	
	
  }]);