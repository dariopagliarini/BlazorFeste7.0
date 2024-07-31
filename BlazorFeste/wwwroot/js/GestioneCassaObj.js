"use strict";

export var GestioneCassaObj = {
  objRef: null,

  webAppAttiva: false,
  confirmPopup: null,
  toolbarCassa: null,
  toolbarScontrino: null,
  gridOrdini: null,
  store: null,
  Prodotti: null,
  arrayTavoli: null,
  arrayCoperti: null,
  scontrino_data: null,
  scontrino_store: null,
  idUltimoOrdine: null,

  cbStampaAbilitata: null,
  cbChiediConferma: null,

  btnRistampaOrdine: null,

  btnServito: null,
  btnBanco: null,

  switchPagamentoConPOS: null,
  PagamentoConPOS: null,
  POS_Disponibile: null,
  RichiestaCreazioneOrdineInCorso: false,

  popupGetFromCloud: null,
  edtOrdineDaApp: null,
  gridOrdini: null,
  edtOrdineDaQRCode: null,
  APPIdOrdine: null,

  edtContanti: null,
  edtResto: null,

  btnCancellaOrdine: null,

  flagStampaAbilitata: true,
  flagChiediConferma: false,

  edtTavolo: "",
  edtCoperti: "",
  edtReferente: "",
  edtNote: "",

  edtAPPIdOrdine: 0,

  init: (_objRef, _Cassa, _WebAppAttiva, _IsDevelopment) => {
    console.log(moment().format("HH:mm:ss.SSS") + " - " + " GestioneCassaObj - init - Ordini: " + _Cassa.ordiniDellaCassa + " (" + _Cassa.idUltimoOrdine + ")");

    document.addEventListener('keydown', GestioneCassaObj.keyHandler);

    GestioneCassaObj.objRef = _objRef;
    GestioneCassaObj.webAppAttiva = _WebAppAttiva;

    GestioneCassaObj.idUltimoOrdine = _Cassa.idUltimoOrdine;

    GestioneCassaObj.flagStampaAbilitata = _Cassa.scontrinoAbilitato;
    console.log(moment().format("HH:mm:ss.SSS") + " - " + " GestioneCassaObj - init - flagStampaAbilitata: " + GestioneCassaObj.flagStampaAbilitata);

    GestioneCassaObj.arrayTavoli = [];
    for (var i = 1; i <= 120; i++) {
      GestioneCassaObj.arrayTavoli.push(i);
    }
    GestioneCassaObj.arrayCoperti = [];
    for (var i = 1; i <= 32; i++) {
      GestioneCassaObj.arrayCoperti.push(i);
    }

    $("#confirmPopup").dxPopup({
      contentTemplate: function () { return $('<div>').append($("<p>Cosa devo stampare?</p>")); },
      width: 300,
      height: "auto",
      showTitle: true,
      title: 'Confermare la richiesta',
      visible: false,
      dragEnabled: false,
      hideOnOutsideClick: true,
      showCloseButton: true,
      position: {
        at: 'center',
        my: 'center',
      },
      toolbarItems: [{
        widget: 'dxButton',
        toolbar: 'bottom',
        location: 'center',
        options: {
          icon: 'user',
          text: 'Cassa',
          onClick() {
            _objRef.invokeMethodAsync("OnPrintRequest_Consumi", false)
              .catch(err => console.error(err.toString()));
            GestioneCassaObj.confirmPopup.hide();
          },
        },
      },
      {
        widget: 'dxButton',
        toolbar: 'bottom',
        location: 'center',
        options: {
          icon: 'runner',
          text: 'Globale',
          onClick() {
            _objRef.invokeMethodAsync("OnPrintRequest_Consumi", true)
              .catch(err => console.error(err.toString()));
            GestioneCassaObj.confirmPopup.hide();
          },
        },
      }
      ],
      onInitialized(e) {
        GestioneCassaObj.confirmPopup = e.component;
      },
    });

    GestioneCassaObj.popupGetFromCloud = $("#myGetFromCloudPopup").dxPopup({
      visible: false,
      title: 'Importa Ordine',
      position: {
        my: 'center top',
        at: 'center top',
        of: window
      },
      dragEnabled: true,
      contentTemplate: function (e) {
        e.append(
          $("<table style='width:100%'>").append(
            $("<tr style='vertical-align:top'>").append(
              $("<td>").append(
                $("<div />").attr("id", "edtOrdineDaApp").dxNumberBox({
                  label: "Ordine da Identificativo",
                  stylingMode: "filled",
                  mode: "number",
                  step: 1,
                  width: 236,
                  height: 56,
                  inputAttr: {
                    style: "font-size: 18px"
                  },
                  onInitialized(e) {
                    GestioneCassaObj.edtOrdineDaApp = e.component;
                  },
                  onEnterKey: function () {
                    GestioneCassaObj.APPIdOrdine = GestioneCassaObj.edtOrdineDaApp.option("value");
                    GestioneCassaObj.btnCancellaOrdineOnClick();

                    _objRef.invokeMethodAsync("OnGetOrderFromCloud_Async", GestioneCassaObj.APPIdOrdine)
                      .then(_retvalue => {
                        try {
                          if (GestioneCassaObj.APPIdOrdine > 0) {
                            var retvalue = JSON.parse(_retvalue);

                            GestioneCassaObj.JsonDaCloud(retvalue);
                            GestioneCassaObj.popupGetFromCloud.hide();
                          } else if (GestioneCassaObj.APPIdOrdine == 0) {
                            var retvalue = JSON.parse(_retvalue);

                            GestioneCassaObj.gridOrdini.beginUpdate();
                            GestioneCassaObj.gridOrdini.option("dataSource", retvalue);
                            GestioneCassaObj.gridOrdini.endUpdate();
                          } else if (GestioneCassaObj.APPIdOrdine < 0) {
                            var retvalue = JSON.parse(_retvalue);

                            GestioneCassaObj.gridOrdini.beginUpdate();
                            GestioneCassaObj.gridOrdini.option("dataSource", retvalue);
                            GestioneCassaObj.gridOrdini.endUpdate();
                          }
                        } catch (error) {
                        }
                      });
                  }
                }),
                $("<div />").attr("id", "gridOrdini").dxDataGrid({
                  dataSource: [],
                  height: "480px",
                  keyExpr: "id",
                  noDataText: "...",
                  //focusedRowEnabled: true,
                  //focusedRowIndex: 0, // focus the first row
                  rowAlternationEnabled: true,
                  showRowLines: true,
                  showBorders: true,
                  hoverStateEnabled: true,
                  //                  sorting: { mode: "none" },
                  headerFilter: {
                    visible: true,
                  },
                  sorting: {
                    mode: 'single',
                  },
                  selection: {
                    mode: 'single',
                  },
                  scrolling: {
                    mode: "virtual"
                  },
                  columns: [
                    {
                      type: "buttons",
                      width: _IsDevelopment ? 70 : 35,
                      buttons: [
                        {
                          icon: "import",
                          hint: "Importa Ordine",
                          onClick: function (e) {
                            GestioneCassaObj.APPIdOrdine = e.row.data.id;
                            GestioneCassaObj.btnCancellaOrdineOnClick();

                            _objRef.invokeMethodAsync("OnGetOrderFromCloud_Async", GestioneCassaObj.APPIdOrdine)
                              .then(_retvalue => {
                                try {
                                  var retvalue = JSON.parse(_retvalue);

                                  GestioneCassaObj.JsonDaCloud(retvalue);
                                  GestioneCassaObj.popupGetFromCloud.hide();
                                } catch (error) {
                                }
                              });
                          }
                        },
                        {
                          icon: "exportselected",
                          hint: "Evadi Ordine",
                          visible: _IsDevelopment,
                          onClick: function (e) {
                            var result = DevExpress.ui.dialog.confirm("<i>Confermi l'operazione sull'ordine selezionato?</i>", "Evasione Ordine");
                            result.done(function (dialogResult) {
                              if (dialogResult) {
                                _objRef.invokeMethodAsync("OnEvadiOrderFromCloud_Async", e.row.data.id);

                                // Aspetto alcuni millisecondi prima di fare il refresh della grid
                                setTimeout(function () {
                                  _objRef.invokeMethodAsync("OnGetOrderFromCloud_Async", 0)
                                    .then(_retvalue => {
                                      try {
                                        var retvalue = JSON.parse(_retvalue);

                                        GestioneCassaObj.gridOrdini.beginUpdate();
                                        GestioneCassaObj.gridOrdini.option("dataSource", retvalue);
                                        GestioneCassaObj.gridOrdini.endUpdate();
                                      } catch (error) {
                                      }
                                    });
                                }, 250);
                              }
                            });
                          }
                        },
                      ]
                    },
                    {
                      caption: "#",
                      dataField: 'id',
                      dataType: "number",
                      width: 70,
                      alignment: 'center',
                      allowFiltering: false,
                    },
                    {
                      caption: "Tavolo",
                      dataField: 'table',
                      dataType: "number",
                      width: 120,
                      alignment: 'center',
                      cellTemplate: function (element, info) {
                        $("<div style='font-size:13px; font-weight:bold'>" + info.value + "</div>").appendTo(element);
                      },
                    },
                    {
                      caption: "Coperti",
                      dataField: 'coperti',
                      alignment: 'center',
                      width: 70,
                      allowFiltering: false,
                      allowSorting: false,
                      cellTemplate: function (element, info) {
                        $("<div style='font-size:13px; font-weight:bold; padding-left: 6px;text-overflow: ellipsis; overflow: hidden;'>" + info.value + "</div>").appendTo(element);
                      },
                    },
                    {
                      caption: "Referente al tavolo",
                      dataField: 'referente',
                      width: 140,
                      allowFiltering: false,
                      allowSorting: false,
                      cellTemplate: function (element, info) {
                        $("<div style='font-size:13px; font-weight:bold; padding-left: 6px;text-overflow: ellipsis; overflow: hidden;'>" + info.value + "</div>").appendTo(element);
                      },
                    },
                    {
                      caption: "Data Ordine",
                      dataField: 'dataCloud',
                      alignment: 'center',
                      width: 160,
                      allowFiltering: false,
                      allowSorting: false,
                    },
                    {
                      caption: "Note Ordine",
                      dataField: 'note',
                      width: _IsDevelopment ? 140 : 170,
                      allowFiltering: false,
                      allowSorting: false,
                    },
                  ],
                  onInitialized: function (e) {
                    GestioneCassaObj.gridOrdini = e.component;
                  },
                  onRowPrepared: function (e) {
                    switch (e.rowType) {
                      case "header":
                        //e.rowElement.css({ height: 40 });
                        break;
                      case "data":
                        //e.rowElement.css({ height: 33, "color" : "white", "background-color" : "red"});
                        break;
                    }
                  },
                  onCellDblClick: function (e) {
                    GestioneCassaObj.APPIdOrdine = e.key;
                    GestioneCassaObj.btnCancellaOrdineOnClick();

                    _objRef.invokeMethodAsync("OnGetOrderFromCloud_Async", GestioneCassaObj.APPIdOrdine)
                      .then(_retvalue => {
                        try {
                          var retvalue = JSON.parse(_retvalue);

                          GestioneCassaObj.JsonDaCloud(retvalue);
                          GestioneCassaObj.popupGetFromCloud.hide();
                        } catch (error) {
                        }
                      });
                  }
                })
              ),
              $("<td>").append(
                $("<div />").attr("id", "edtOrdineDaQRCode").dxTextBox({
                  label: "Ordine da QRCode",
                  stylingMode: "filled",
                  width: 236,
                  height: 56,
                  inputAttr: {
                    style: "font-size: 12px"
                  },
                  onInitialized: function (e) {
                    GestioneCassaObj.edtOrdineDaQRCode = e.component;
                  },
                  onEnterKey: function () {
                    GestioneCassaObj.btnCancellaOrdineOnClick();

                    var _retvalue = GestioneCassaObj.edtOrdineDaQRCode.option("value");
                    try {
                      var retvalue = JSON.parse(_retvalue);

                      GestioneCassaObj.JsonDaCloud(retvalue);
                      GestioneCassaObj.popupGetFromCloud.hide();
                    } catch (error) {
                    }
                  }
                })
              )
            )
          )
        )
      },
      onShowing: function (e) {
        GestioneCassaObj.edtOrdineDaApp.option("value", "");
        GestioneCassaObj.edtOrdineDaQRCode.option("value", "");
        GestioneCassaObj.gridOrdini.option("dataSource", []);
        _objRef.invokeMethodAsync("OnGetOrderFromCloud_Async", 0)
          .then(_retvalue => {
            try {
              var retvalue = JSON.parse(_retvalue);

              GestioneCassaObj.gridOrdini.beginUpdate();
              GestioneCassaObj.gridOrdini.option("dataSource", retvalue);
              GestioneCassaObj.gridOrdini.endUpdate();
            } catch (error) {
            }
          });
      },
    }).dxPopup("instance");

    GestioneCassaObj.PagamentoConPOS = false;
    GestioneCassaObj.POS_Disponibile = _Cassa.pos;

    GestioneCassaObj.scontrino_data = [];
    GestioneCassaObj.scontrino_store = new DevExpress.data.ArrayStore({
      key: "idProdotto",
      data: GestioneCassaObj.scontrino_data,
      onInserted: function (values, key) {
        GestioneCassaObj.gridScontrino.refresh();
        GestioneCassaObj.updateButtonState();
        GestioneCassaObj.calcolaResto();
      },
      onUpdated: function (key, values) {
        GestioneCassaObj.gridScontrino.refresh();
        GestioneCassaObj.updateButtonState();
        GestioneCassaObj.calcolaResto();
      },
      onRemoved: function (key) {
        GestioneCassaObj.gridScontrino.refresh();
        GestioneCassaObj.updateButtonState();
        GestioneCassaObj.calcolaResto();
      }
    });

    $('#toolbarCassa').dxToolbar({
      items: [
        {
          location: 'before',
          widget: 'dxButton',
          visible: _Cassa.scontrinoAbilitato,
          options: {
            icon: 'print',
            onClick(e) {
              var result = DevExpress.ui.dialog.confirm("<i>Confermi l'operazione?</i>", "Scontrino di Prova");
              result.done(function (dialogResult) {
                if (dialogResult) {
                  e.component.option("visible", false);

                  _objRef.invokeMethodAsync("OnPrintRequest_StampaDiProva")
                    .catch(err => console.error(err.toString()));
                }
              });
            },
          },
        },
        {
          location: 'before',
          locateInMenu: 'never',
          template() {
            return $("<div class='toolbar-label' style='padding-right:12px'><b>Gestione " + _Cassa.cassa + "</b></div>");
          },
        },
        {
          location: 'before',
          widget: 'dxCheckBox',
          visible: _Cassa.scontrinoAbilitato,
          options: {
            value: GestioneCassaObj.flagStampaAbilitata,
            text: "Stampa Scontrino",

            onInitialized: function (e) {
              GestioneCassaObj.cbStampaAbilitata = e.component;
            },

            onValueChanged: function (e) {
              GestioneCassaObj.flagStampaAbilitata = e.value;

              GestioneCassaObj.btnRistampaOrdine.option("disabled", !e.value);
            }
          },
        },
        {
          location: 'before',
          widget: 'dxCheckBox',
          options: {
            value: GestioneCassaObj.flagChiediConferma,
            text: "Chiedi Conferma",

            onInitialized: function (e) {
              GestioneCassaObj.cbChiediConferma = e.component;
            },

            onValueChanged: function (e) {
              GestioneCassaObj.flagChiediConferma = e.value;
            }
          },
        },
        {
          location: 'after',
          template() {
            return $('<div style="padding-right:20px">')
              .addClass('informer')
              .append(
                $('<div>')
                  .html('<div>Contanti</div>'),
                $('<span>')
                  .addClass('cassaContanti')
                  .html('<b>0.0</b> €'),
              );

          },
        },
        {
          location: 'after',
          template() {
            return $('<div style="padding-right:20px">')
              .addClass('informer')
              .append(
                $('<div>')
                  .html('<div>POS</div>'),
                $('<span>')
                  .addClass('cassaPOS')
                  .html('<b>0.0</b> €'),
              );

          },
        },
        {
          location: 'after',
          widget: 'dxButton',
          visible: GestioneCassaObj.webAppAttiva && _Cassa.soloBanco == false,
          options: {
            icon: 'fa-solid fa-list',
            type: 'giallo',
            hint: "Ordine da Identificativo",
            onClick(e) {
              GestioneCassaObj.popupGetFromCloud.option("width", 820);
              GestioneCassaObj.popupGetFromCloud.option("height", 640);
              GestioneCassaObj.popupGetFromCloud.show();
              setTimeout(function () {
                GestioneCassaObj.edtOrdineDaQRCode.option("visible", false);
                GestioneCassaObj.edtOrdineDaApp.option("visible", false);
                GestioneCassaObj.gridOrdini.option("visible", true);
              }, 0);
              setTimeout(function () {
                GestioneCassaObj.gridOrdini.focus();
              }, 500);
            },
          },
        },
        {
          location: 'after',
          widget: 'dxButton',
          visible: GestioneCassaObj.webAppAttiva && _Cassa.soloBanco == false,
          options: {
            icon: 'fa-solid fa-qrcode',
            type: 'giallo',
            hint: "Ordine da QRCode",
            onClick(e) {
              GestioneCassaObj.popupGetFromCloud.option("width", 300);
              GestioneCassaObj.popupGetFromCloud.option("height", 160);
              GestioneCassaObj.popupGetFromCloud.show();
              setTimeout(function () {
                GestioneCassaObj.edtOrdineDaApp.option("visible", false);
                GestioneCassaObj.gridOrdini.option("visible", false);
                GestioneCassaObj.edtOrdineDaQRCode.option("visible", true);
              }, 0);
              setTimeout(function () {
                GestioneCassaObj.edtOrdineDaQRCode.focus();
              }, 500);
            },
          },
        },
        {
          location: 'after',
          widget: 'dxButton',
          visible: _Cassa.scontrinoAbilitato,
          options: {
            disabled: (GestioneCassaObj.idUltimoOrdine === 0),
            icon: 'fa-solid fa-print',
            type: 'default',
            text: 'Ristampa',
            hint: "Ristampa Ultimo Ordine",

            onInitialized: function (e) {
              GestioneCassaObj.btnRistampaOrdine = e.component;
            },

            onClick() {
              var result = DevExpress.ui.dialog.confirm("<i>Confermi l'operazione?</i>", "Ristampa Scontrino");
              result.done(function (dialogResult) {
                if (dialogResult) {
                  _objRef.invokeMethodAsync("OnPrintRequest",
                    GestioneCassaObj.flagStampaAbilitata,
                    GestioneCassaObj.idUltimoOrdine)
                    .catch(err => console.error(err.toString()));
                }
              });
            },
          },
        },
        {
          location: 'after',
          widget: 'dxButton',
          visible: _Cassa.scontrinoAbilitato,
          options: {
            elementAttr: { id: 'btnStampaConsumi' },
            disabled: false,
            icon: 'fa-solid fa-print',
            type: 'default',
            text: 'Consumi',
            hint: "Stampa Consumi",
            onClick() {
              GestioneCassaObj.confirmPopup.show();
            },
          },
        },
        {
          location: 'after',
          locateInMenu: 'never',
          template() {
            return $("<div id='lblDataOra' class='toolbar-label'>-</div>");
          },
        },
      ],
      onInitialized(e) {
        GestioneCassaObj.toolbarCassa = e.component;
      },
    });

    $('#toolbarScontrino').dxToolbar({
      items: [
        {
          location: 'before',
          widget: 'dxButton',
          options: {
            width: 65,
            height: 40,
            type: 'giallo',
            text: 'CANCELLA ORDINE',
            hint: "Azzera Dati Ordine",
            onInitialized(e) {
              GestioneCassaObj.btnCancellaOrdine = e.component;
            },
            onClick(e) {
              var result = DevExpress.ui.dialog.confirm("<i>Sei proprio sicuro/a?</i>", "Cancella Ordine");
              result.done(function (dialogResult) {
                if (dialogResult) {
                  GestioneCassaObj.btnCancellaOrdineOnClick();
                }
              });
            },
          },
        },
        {
          location: 'before',
          widget: 'dxSwitch',
          options: {
            elementAttr: { id: 'switchPagamentoConPOS' },
            switchedOffText: "Contanti",
            switchedOnText: "POS",
            height: 40,
            width: 64,
            onInitialized: function (e) {
              GestioneCassaObj.switchPagamentoConPOS = e.component;
              e.component.option("value", GestioneCassaObj.PagamentoConPOS);
              e.component.option("disabled", !GestioneCassaObj.POS_Disponibile);
            },
            onValueChanged: function (e) {
              GestioneCassaObj.PagamentoConPOS = e.value;

              GestioneCassaObj.edtContanti.option("visible", GestioneCassaObj.switchPagamentoConPOS.option("visible") && (!GestioneCassaObj.PagamentoConPOS));
              GestioneCassaObj.edtResto.option("visible", GestioneCassaObj.switchPagamentoConPOS.option("visible") && (!GestioneCassaObj.PagamentoConPOS));
            },
          }
        },
        {
          location: 'before',
          widget: 'dxNumberBox',
          options: {
            label: "Contanti [F8]",
            stylingMode: "filled",
            format: { type: "currency", currency: "EUR", precision: 2 },
            min: 0,
            mode: "number",
            step: 5,
            width: 90,
            height: 40,
            elementAttr: {
              id: 'edtContanti',
              style: "margin-top: 0px; background-color: ivory"
            },
            inputAttr: {
              style: "font-size: 18px"
            },
            onInitialized(e) {
              GestioneCassaObj.edtContanti = e.component;
              e.component.option("visible", !GestioneCassaObj.PagamentoConPOS);
            },
            onValueChanged(data) {
              GestioneCassaObj.calcolaResto();
            },
            onFocusIn(e) {
              if (e.component.option("value") == 0) {
                DevExpress.data.query(GestioneCassaObj.scontrino_store._array)
                  .sum("totale")
                  .done(function (result) {
                    e.component.option("value", Math.ceil(result / 5) * 5);
                  });
              }
            },
            onEnterKey: function () {
              GestioneCassaObj.calcolaResto();
            }
          },
        },
        {
          location: 'before',
          widget: 'dxNumberBox',
          options: {
            label: "Resto",
            stylingMode: "filled",
            format: { type: "currency", currency: "EUR", precision: 2 },
            readOnly: true,
            width: 90,
            height: 40,
            elementAttr: {
              id: 'edtResto',
              style: "font-weight: bold; margin-top: 0px; background-color: ivory"
            },
            inputAttr: {
              style: "font-size: 18px; font-weight: bold"
            },
            onInitialized(e) {
              GestioneCassaObj.edtResto = e.component;
              e.component.option("visible", !GestioneCassaObj.PagamentoConPOS);
            },
          },
        },
        {
          location: 'after',
          widget: 'dxButton',
          visible: _Cassa.soloBanco == false,
          options: {
            width: 70,
            height: 40,
            type: 'verde',
            text: 'SERVITO AL TAVOLO',
            hint: "Ordine Servito al tavolo",

            onInitialized(e) {
              GestioneCassaObj.btnServito = e.component;
            },

            onClick(e) {
              if (GestioneCassaObj.RichiestaCreazioneOrdineInCorso == false) {
                GestioneCassaObj.RichiestaCreazioneOrdineInCorso = true;

                if (GestioneCassaObj.flagChiediConferma | GestioneCassaObj.PagamentoConPOS) {
                  var testoConferma = "<i>Confermi l'operazione?</i>"
                  if (GestioneCassaObj.PagamentoConPOS) {
                    testoConferma = "<i>Confermi che il Pagamento Elettronico è andato a buon fine?</i>"
                  }
                  var result = DevExpress.ui.dialog.confirm(testoConferma, "Chiusura Ordine");
                  result.done(function (dialogResult) {
                    if (dialogResult) {
                      GestioneCassaObj.btnSaveToMySQL('SERVITO');
                    } else {
                      GestioneCassaObj.RichiestaCreazioneOrdineInCorso = false;
                    }
                  });
                } else {
                  GestioneCassaObj.btnSaveToMySQL('SERVITO');
                }
              } else {
                console.log(moment().format("HH:mm:ss.SSS") + " - " + " GestioneCassaObj - onClick - SERVITO: Click con ordine già in creazione");
              }
            },
          },
        },
        {
          location: 'after',
          widget: 'dxButton',
          options: {
            width: 70,
            height: 40,
            type: 'verde',
            text: 'RITIRO AL BANCO',
            hint: "Ordine Ritirato al Banco",
            onInitialized(e) {
              GestioneCassaObj.btnBanco = e.component;
            },
            onClick(e) {
              if (GestioneCassaObj.RichiestaCreazioneOrdineInCorso == false) {
                GestioneCassaObj.RichiestaCreazioneOrdineInCorso = true;

                if (GestioneCassaObj.flagChiediConferma | GestioneCassaObj.PagamentoConPOS) {
                  var testoConferma = "<i>Confermi l'operazione?</i>"
                  if (GestioneCassaObj.PagamentoConPOS) {
                    testoConferma = "<i>Confermi che il Pagamento Elettronico è andato a buon fine?</i>"
                  }
                  var result = DevExpress.ui.dialog.confirm(testoConferma, "Chiusura Ordine");
                  result.done(function (dialogResult) {
                    if (dialogResult) {
                      GestioneCassaObj.btnSaveToMySQL('BANCO');
                    }
                  });
                } else {
                  GestioneCassaObj.btnSaveToMySQL('BANCO');
                }
              } else {
                console.log(moment().format("HH:mm:ss.SSS") + " - " + " GestioneCassaObj - onClick - BANCO: Click con ordine già in creazione");
              }
            },
          },
        },
      ],
      onInitialized(e) {
        GestioneCassaObj.toolbarScontrino = e.component;
      },
    });

    $("#gridScontrino").dxDataGrid({
      dataSource: GestioneCassaObj.scontrino_store,
      showColumnHeaders: true,
      height: "100%",
      width: "100%",
      columns: [
        {
          caption: "#", dataField: "idProdotto", alignment: "center", width: 30, visible: false, sortIndex: 0, sortOrder: "asc"
        },
        {
          caption: "Prodotto",
          dataField: "nomeProdotto", alignment: "left",
          cellTemplate: function (element, info) {
            $("<div style='font-size:13px'>" + info.value + "</div>").appendTo(element);
          },
        },
        {
          caption: "Qtà",
          dataField: "quantitàProdotto",
          width: 50,
          alignment: "center",
          cellTemplate: function (element, info) {
            $('<span class="quantity-value">' + info.value + '</span>')
              .appendTo(element);
          },
        },
        {
          caption: "Azioni",
          width: 85,
          type: "buttons",
          buttons: [{
            icon: "minus",
            hint: "Decrementa quantità",
            onClick: function (e) {
              GestioneCassaObj.minusClick("decr", e.row.data);
            }
          },
          {
            icon: "trash",
            hint: "Elimina riga dall'ordine",
            onClick: function (e) {
              GestioneCassaObj.minusClick("delete", e.row.data);
            }
          },
          {
            icon: "plus",
            hint: "Incrementa quantità",
            onClick: function (e) {
              GestioneCassaObj.minusClick("incr", e.row.data);
            }
          }
          ]
        },
        {
          caption: "Totale (€)",
          dataField: "totale",
          format: {
            type: "fixedPoint",
            precision: 2
          },
          width: 85,
          alignment: "right",
          cellTemplate: function (element, info) {
            $("<div class='total-value'>" + info.text + "</div>").appendTo(element);
          },
        },
      ],
      summary: {
        totalItems:
          [{
            column: 'nomeProdotto',
            summaryType: 'count',
            alignment: "right",
            customizeText(data) {
              return "Totale:";
            },
          },
          { column: 'quantitàProdotto', summaryType: 'sum', valueFormat: { type: "fixedPoint", precision: 0 }, displayFormat: "{0}" },
          { column: 'totale', summaryType: 'sum', valueFormat: { type: "fixedPoint", precision: 2 }, displayFormat: "{0}" },
          ],
      },
      noDataText: "Selezionare un prodotto con il mouse per iniziare un nuovo ordine. Quando l'ordine è concluso cliccare \"SERVITO AL TAVOLO\" o \"RITIRO AL BANCO\" per chiuderlo e stamparlo. Nel caso di ordine \"SERVITO AL TAVOLO\" ricordate di inserire anche i dati relativi al \"Tavolo\" ed al n° dei \"Coperti\". Il campo \"Signor\" dovrebbe essere compilato in ogni caso.",
      showRowLines: true,
      rowAlternationEnabled: true,
      columnAutoWidth: true,
      wordWrapEnabled: true,
      showBorders: true,
      hoverStateEnabled: true,
      paging: { enabled: false },
      scrolling: { mode: "standard" },
      selection: { mode: 'single', },
      sorting: { mode: "none" },
      onKeyDown: function (e) {
        switch (e.event.key) {
          case "-":
            var selectedData = e.component.getSelectedRowsData();
            if (selectedData.length > 0) {
              GestioneCassaObj.minusClick("decr", selectedData[0]);
            }
            e.handled = true;
            break;

          case "+":
            var selectedData = e.component.getSelectedRowsData();
            if (selectedData.length > 0) {
              GestioneCassaObj.minusClick("incr", selectedData[0]);
            }
            e.handled = true;
            break;

          default:
            break;
        }
      },
      onRowPrepared: function (e) {
        switch (e.rowType) {
          case "header":
            break;

          case "data":
            //e.rowElement.css({ height: "42px" });
            e.rowElement.find("td").css({ "vertical-align": "middle" });
            break;
        }
      },
      onInitialized(e) {
        GestioneCassaObj.gridScontrino = e.component;
      },
    });

    $("#edtTavolo").dxSelectBox({
      label: "Tavolo",
      noDataText: "-",
      showDataBeforeSearch: true,
      searchTimeout: 100,
      showDropDownButton: false,
      acceptCustomValue: true,
      searchEnabled: true,
      dataSource: GestioneCassaObj.arrayTavoli,
      onValueChanged(data) {
        if (data.value) {
          GestioneCassaObj.edtTavolo = data.value.toString().trim();
        } else {
          GestioneCassaObj.edtTavolo = "";
        }
        GestioneCassaObj.updateButtonState();
      },
      onEnterKey: function () {
        $("#edtCoperti").dxSelectBox("instance").focus();
      }
    });

    $("#edtCoperti").dxSelectBox({
      label: "Coperti",
      noDataText: "-",
      showDataBeforeSearch: true,
      searchTimeout: 100,
      showDropDownButton: false,
      acceptCustomValue: true,
      searchEnabled: true,
      dataSource: GestioneCassaObj.arrayCoperti,
      onValueChanged(data) {
        if (data.value) {
          GestioneCassaObj.edtCoperti = data.value.toString().trim();
        } else {
          GestioneCassaObj.edtCoperti = "";
        }
      },
      onEnterKey: function () {
        $("#edtReferente").dxTextBox("instance").focus();
      }
    });

    $("#edtReferente").dxTextBox({
      label: "Signor",
      inputAttr: { 'style': "text-transform: capitalize" },
      onValueChanged: function (data) {
        if (data.value) {
          let testo = data.value.trim();
          GestioneCassaObj.edtReferente = testo.charAt(0).toUpperCase() + testo.slice(1);
        } else {
          GestioneCassaObj.edtReferente = "";
        }
      },
      onEnterKey: function () {
        $("#edtNotaOrdine").dxTextArea("instance").focus();
      }
    });

    $("#edtNotaOrdine").dxTextArea({
      label: "Nota Ordine",
      onValueChanged: function (data) {
        if (data.value) {
          GestioneCassaObj.edtNote = data.value.trim();
        } else {
          GestioneCassaObj.edtNote = "";
        }
      },
    });

    if (_Cassa.soloBanco) {
      document.getElementById('formOrdine').style.visibility = 'hidden';
      document.getElementById('formOrdine').style.height = '0px';

      document.getElementById('gridOrdine').style.height = 'calc(630px - var(--cardTitoloHeight) - var(--cardFormOrdineMargin) - var(--cardtoolbarScontrinoHeight))';
    }

    // All'inizio ripulisco lo scontrino
    GestioneCassaObj.btnCancellaOrdineOnClick();
  },

  dispose: () => {
    document.removeEventListener('keydown', GestioneCassaObj.keyHandler);
  },

  keyHandler: (e) => {
    switch (e.keyCode) {
      case 119: // FN8
        $("#edtContanti").dxNumberBox("instance").focus();
        break;
      default:  //Fallback to default browser behaviour
        return true;
    }
    //Returning false overrides default browser event
    return false;
  },

  createButtons: (_Cassa, _Prodotti) => {
    if (_Prodotti != null) {
      GestioneCassaObj.Prodotti = _Prodotti;

      let K_ROW;
      let K_COL;

      let H_CELL;
      let W_CELL;
      let F_CELL;
      let F_LABEL;

      const numProdotti = _Prodotti.length;
      switch (true) {
        case (numProdotti <= 30):
          K_ROW = 5;
          K_COL = 6;
          F_CELL = 16;
          F_LABEL = 12;
          break;

        case (numProdotti <= 36):
          K_ROW = 6;
          K_COL = 6;
          F_CELL = 14;
          F_LABEL = 12;
          break;

        case (numProdotti <= 42):
          K_ROW = 7;
          K_COL = 6;
          F_CELL = 14;
          F_LABEL = 12;
          break;

        case (numProdotti <= 48):
          K_ROW = 8;
          K_COL = 6;
          F_CELL = 13;
          F_LABEL = 12;
          break;

        case (numProdotti <= 49):
          K_ROW = 7;
          K_COL = 7;
          F_CELL = 13;
          F_LABEL = 12;
          break;

        case (numProdotti <= 56):
          K_ROW = 8;
          K_COL = 7;
          F_CELL = 12;
          F_LABEL = 10;
          break;

        case (numProdotti <= 70):
          K_ROW = 10;
          K_COL = 7;
          F_CELL = 12;
          F_LABEL = 10;
          break;
      }
      H_CELL = Math.trunc((630 / K_ROW) - 2);
      W_CELL = Math.trunc((665 / K_COL) - 2);

      console.log(moment().format("HH:mm:ss.SSS") + " - " + " GestioneCassaObj - createButtons: " + H_CELL + "x" + W_CELL);

      var Index = 0;
      var table = document.createElement('table');

      table.setAttribute('class', 'table table-borderless table-responsive');
      table.setAttribute('style', 'margin-bottom: 0px;');

      for (var iRow = 0; iRow < K_ROW; iRow++) {
        var row = table.insertRow(-1);
        for (var iCol = 0; iCol < K_COL; iCol++) {
          var cell = row.insertCell(-1);
          cell.setAttribute("style", "padding:1px");

          if (Index < _Prodotti.length) {
            let prodotto = _Prodotti[Index];
            let nomeProdotto = prodotto.nomeProdotto.replaceAll("<CR>", "<br>");

            var div = document.createElement('div');
            div.setAttribute("id", "btnProdotto_" + prodotto.idProdotto);
            div.setAttribute("class", "p-0");

            let strStyle = "position:relative;overflow:hidden;font-size:" + F_CELL + "px;font-weight:bold; color:" + prodotto.foreColor + ";background-color: " + prodotto.backColor + "; width:" + W_CELL + "px; height:" + H_CELL + "px";

            if (prodotto.stato) {
              let strMagazzino = '<div title="Magazzino" style="font-size:' + F_LABEL + 'px" id="lblMagazzino_' + prodotto.idProdotto + '" class="child_top_right ' + (prodotto.magazzino > 0 ? 'visible" >' : 'invisible" >') + (prodotto.magazzino - prodotto.consumoCumulativo) + '</div>';

              let strDaEvadere = '';
              if (prodotto.viewLableDaEvadere) {
                strDaEvadere = '<div title="Da Evadere" style="font-size:' + F_LABEL + 'px" id="lblDaEvadere_' + prodotto.idProdotto + '" class="child_lblDaEvadere" >' + (prodotto.consumo - prodotto.evaso) + '</div>';
              }

              div.innerHTML = "<button class='btn btn-sm border p-0' style='" + strStyle + "'>" + nomeProdotto + strMagazzino + strDaEvadere + "</button>";
              div.addEventListener("click", function () { GestioneCassaObj.btnClicked(prodotto); }, false);
            } else {
              div.innerHTML = "<button class='btn btn-sm border p-0' style='" + strStyle + "' disabled>" + nomeProdotto + "</button>";
            }
            cell.append(div);
            //console.log(moment().format("HH:mm:ss.SSS") + " - " + " GestioneCassaObj - createButtons 1: " + Index + "/" + _Prodotti.length);
          } else {
            var div = document.createElement('div');
            div.setAttribute("id", "btnProdottoXXX_" + Index);
            div.setAttribute("class", "p-0");

            let strStyle = "position:relative;overflow:hidden;font-size:" + F_CELL + "px;font-weight:bold; background-color: silver; width:" + W_CELL + "px; height:" + H_CELL + "px";

            div.innerHTML = "<button class='btn btn-sm border p-0' style='" + strStyle + "' disabled></button>";
            cell.append(div);

            //console.log(moment().format("HH:mm:ss.SSS") + " - " + " GestioneCassaObj - createButtons 2: " + Index + "/" + _Prodotti.length);
          }
          Index++;
        }
      }
      $("#tableProdotti").empty()
      $("#tableProdotti").append(table);
    }
  },

  updateButtonState: () => {
    if (GestioneCassaObj.gridScontrino != undefined) {
      GestioneCassaObj.scontrino_store.totalCount()
        .done(function (count) {
          if (GestioneCassaObj.edtTavolo === "") {
            GestioneCassaObj.btnBanco.option("visible", count > 0);
            GestioneCassaObj.btnServito.option("visible", false);
          } else {
            GestioneCassaObj.btnBanco.option("visible", false);
            GestioneCassaObj.btnServito.option("visible", count > 0);
          }
          GestioneCassaObj.switchPagamentoConPOS.option("visible", count > 0);
          GestioneCassaObj.btnCancellaOrdine.option("disabled", count <= 0);

          GestioneCassaObj.edtContanti.option("visible", GestioneCassaObj.switchPagamentoConPOS.option("visible") && (!GestioneCassaObj.PagamentoConPOS));
          GestioneCassaObj.edtResto.option("visible", GestioneCassaObj.switchPagamentoConPOS.option("visible") && (!GestioneCassaObj.PagamentoConPOS));
        })
        .fail(function (error) {
          // Handle the "error" here
          GestioneCassaObj.btnCancellaOrdine.option("disabled", true);
          GestioneCassaObj.switchPagamentoConPOS.option("visible", false);
          GestioneCassaObj.edtContanti.option("visible", false);
          GestioneCassaObj.edtResto.option("visible", false);

          GestioneCassaObj.btnServito.option("visible", false);
          GestioneCassaObj.btnBanco.option("visible", false);
        });
    }
  },

  btnClicked: (_prodotto) => {
    GestioneCassaObj.scontrino_store.byKey(_prodotto.idProdotto)
      .done(function (dataItem) {
        // Process the "dataItem" here
        dataItem.quantitàProdotto = dataItem.quantitàProdotto + 1;
        dataItem.totale = dataItem.prezzoUnitario * dataItem.quantitàProdotto;

        GestioneCassaObj.scontrino_store.update(dataItem.idProdotto, dataItem);
      })
      .fail(function (error) {
        // Devo aggiungere un nuovo prodotto
        GestioneCassaObj.scontrino_store.insert({
          idProdotto: _prodotto.idProdotto,
          nomeProdotto: _prodotto.nomeProdotto.replaceAll("<CR>", " "),
          quantitàProdotto: 1,
          totale: _prodotto.prezzoUnitario,
          prezzoUnitario: _prodotto.prezzoUnitario
        }).done(function (dataObj, key) {
          // Process the key and data object here
        })
          .fail(function (error) {
            // Handle the "error" here
          });
      });
  },

  btnCancellaOrdineOnClick: () => {
    GestioneCassaObj.scontrino_store.clear();

    GestioneCassaObj.edtTavolo = "";
    GestioneCassaObj.edtCoperti = "";
    GestioneCassaObj.edtReferente = "";
    GestioneCassaObj.edtNote = "";

    GestioneCassaObj.PagamentoConPOS = false;
    GestioneCassaObj.RichiestaCreazioneOrdineInCorso = false;

    GestioneCassaObj.edtAPPIdOrdine = 0;

    $("#edtTavolo").dxSelectBox("instance").reset();
    $("#edtCoperti").dxSelectBox("instance").reset();
    $("#edtReferente").dxTextBox("instance").reset();
    $("#edtNotaOrdine").dxTextArea("instance").reset();

    $("#edtTavolo").dxSelectBox("instance").focus();

    GestioneCassaObj.gridScontrino.refresh();
    GestioneCassaObj.updateButtonState();

    $("#edtContanti").dxNumberBox("instance").option("value", 0);

    GestioneCassaObj.switchPagamentoConPOS.option("value", GestioneCassaObj.PagamentoConPOS);

    GestioneCassaObj.calcolaResto();
  },

  btnSaveToMySQL: (strTipoOrdine) => {
    var righeOrdine = GestioneCassaObj.scontrino_store._array;

    //GestioneCassaObj.btnServito.option("disabled", true);
    //GestioneCassaObj.btnBanco.option("disabled", true);

    DevExpress.data.query(righeOrdine)
      .filter(["quantitàProdotto", ">", 0])
      .count()
      .done(function (result) {
        if (result > 0) {
          GestioneCassaObj.objRef.invokeMethodAsync("OnSaveToMySQLAsync",
            GestioneCassaObj.flagStampaAbilitata,
            strTipoOrdine,
            GestioneCassaObj.edtTavolo,
            GestioneCassaObj.edtCoperti,
            GestioneCassaObj.edtNote,
            GestioneCassaObj.edtReferente,
            GestioneCassaObj.PagamentoConPOS,
            GestioneCassaObj.edtAPPIdOrdine,
            righeOrdine)
            .then(_retvalue => {
              var retvalue = JSON.parse(_retvalue);

              if ("ultimoOrdine" in retvalue) {
                var _idUltimoOrdine = retvalue["ultimoOrdine"];

                GestioneCassaObj.idUltimoOrdine = _idUltimoOrdine;
                GestioneCassaObj.btnRistampaOrdine.option("disabled", ((_idUltimoOrdine === 0) || (!GestioneCassaObj.flagStampaAbilitata)));
              }

              if ("cassaContanti" in retvalue) {
                var _cassaContanti = retvalue["cassaContanti"];

                $('.informer .cassaContanti').html("<b>" + _cassaContanti.toFixed(2) + "</b> €");
              }

              if ("cassaPOS" in retvalue) {
                var _cassaPOS = retvalue["cassaPOS"];

                $('.informer .cassaPOS').html("<b>" + _cassaPOS.toFixed(2) + "</b> €");
              }

              let _resto = $("#edtResto").dxNumberBox("instance").option("value");
              if (_resto > 0) {
                DevExpress.ui.notify(
                  {
                    message: "Ricordati di dare il resto (" + $("#edtResto").dxNumberBox("instance").option("text") + ")",
                    height: 60,
                    width: 240,
                    minWidth: 240,
                    type: 'info',
                    displayTime: 3000,
                  },
                  {
                    position: 'center',
                    direction: 'left-push',
                  });
              }
              GestioneCassaObj.btnCancellaOrdineOnClick();
            })
            .catch(err => {
//              console.error(err.toString())
            }
          )
        } else {
          GestioneCassaObj.btnCancellaOrdineOnClick();
          DevExpress.ui.notify("Nessuna riga valida - Ordine Annullato", "error", 2500);
        }
        GestioneCassaObj.btnServito.option("disabled", false);
        GestioneCassaObj.btnBanco.option("disabled", false);
      });
  },

  updateStatoProdotti: (_Prodotti) => {
    if (_Prodotti != null) {
      _Prodotti.forEach(prodotto => {
        $('#lblMagazzino_' + prodotto.idProdotto).text(prodotto.magazzino - prodotto.consumoCumulativo);
        $('#lblDaEvadere_' + prodotto.idProdotto).text(prodotto.consumo - prodotto.evaso);
      });
    }
  },

  updateAnagrProdotti: (_Cassa, _Prodotti) => {
    if (_Prodotti != null) {
      GestioneCassaObj.createButtons(_Cassa, _Prodotti);

      var righeOrdine = GestioneCassaObj.scontrino_store._array;
      DevExpress.data.query(righeOrdine)
        .filter(["quantitàProdotto", ">", 0])
        .count()
        .done(function (result) {
          if (result > 0) {
            DevExpress.ui.notify("Variazione Anagrafica Prodotti - Verificare l'ordine in corso", "warning", 6000);
          } else {
            DevExpress.ui.notify("Variazione Anagrafica Prodotti", "success", 4000);
          }
        });
    }
  },

  updateDataOra: (_adesso) => {
    $('#lblDataOra').text(_adesso);
  },

  JsonDaCloud: (retvalue) => {
    var aaa = moment(retvalue.dataCloud).local().format("YYYY-MM-DD HH:mm:ss");

    $("#edtTavolo").dxSelectBox("instance").option("value", retvalue.tavolo);
    $("#edtCoperti").dxSelectBox("instance").option("value", retvalue.coperti);
    $("#edtReferente").dxTextBox("instance").option("value", retvalue.referente);
    $("#edtNotaOrdine").dxTextArea("instance").option("value", retvalue.note);

    //GestioneCassaObj.edtTavolo = retvalue.tavolo;
    //GestioneCassaObj.edtCoperti = retvalue.coperti;
    //GestioneCassaObj.edtReferente = retvalue.referente;

    GestioneCassaObj.PagamentoConPOS = (retvalue.pos == 1);
    GestioneCassaObj.edtAPPIdOrdine = retvalue.orderId;

    retvalue.righe.forEach(riga => {
      var prodotto = GestioneCassaObj.Prodotti.find(o => o.idProdotto === riga.id);
      if (prodotto) {
        if (prodotto.stato) {
          for (var i = 0; i < riga.qta; i++) {
            GestioneCassaObj.btnClicked(prodotto);
          }
        }
      }
    });

    GestioneCassaObj.switchPagamentoConPOS.option("value", GestioneCassaObj.PagamentoConPOS);
    GestioneCassaObj.switchPagamentoConPOS.option("disabled", !GestioneCassaObj.POS_Disponibile);
    GestioneCassaObj.edtContanti.option("visible", GestioneCassaObj.switchPagamentoConPOS.option("visible") && (!GestioneCassaObj.PagamentoConPOS));
    GestioneCassaObj.edtResto.option("visible", GestioneCassaObj.switchPagamentoConPOS.option("visible") && (!GestioneCassaObj.PagamentoConPOS));

    GestioneCassaObj.updateButtonState();

    $("#edtNotaOrdine").dxTextArea("instance").focus();
  },

  calcolaResto: () => {
    let _contanti = $("#edtContanti").dxNumberBox("instance").option("value");

    if (_contanti > 0) {
      DevExpress.data.query(GestioneCassaObj.scontrino_store._array)
        .sum("totale")
        .done(function (result) {
          $("#edtResto").dxNumberBox("instance").option("value", _contanti - result);
        });
    } else {
      $("#edtResto").dxNumberBox("instance").option("value", 0);
    }
  },

  minusClick: (_azione, _prodotto) => {
    GestioneCassaObj.scontrino_store.byKey(_prodotto.idProdotto)
      .done(function (dataItem) {
        // Process the "dataItem" here
        switch (_azione) {
          case "delete":
            GestioneCassaObj.scontrino_store.remove(dataItem.idProdotto);
            break;

          case "decr":
            dataItem.quantitàProdotto = dataItem.quantitàProdotto - 1;
            if (dataItem.quantitàProdotto < 0) {
              GestioneCassaObj.scontrino_store.remove(dataItem.idProdotto);
            } else {
              dataItem.totale = dataItem.prezzoUnitario * dataItem.quantitàProdotto;
              GestioneCassaObj.scontrino_store.update(dataItem.idProdotto, dataItem);
            }
            break;

          case "incr":
            dataItem.quantitàProdotto = dataItem.quantitàProdotto + 1;
            dataItem.totale = dataItem.prezzoUnitario * dataItem.quantitàProdotto;
            GestioneCassaObj.scontrino_store.update(dataItem.idProdotto, dataItem);
            break;

          default:
            break;
        }
      })
      .fail(function (error) {
        // Handle the "error" here
      });
  },
}

