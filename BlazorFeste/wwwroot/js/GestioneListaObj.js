"use strict";
import { resyncSelectBox } from "./TopBarObj.js"

export var GestioneListaObj = {
  objRef: null,
  Lista: null,
  toolbar: null,
  gridHeader: null,
  gridOrdini: null,

  editedColumn: null,

  Ordine: null,
  popupEditOrdine: null,
  formEditOrdine: null,
  OrdineToBeUpdated: null,
  btnSbloccaListe: null,

  idTimerUpdateRequest: null,
  //startTime: null,
  //endTime: null,

  flagInizializza : true,
  CSS_RigaTotali: 1,
  Filtro_StatoRiga: false,
  EvasioneParziale: false,

  init: (_objRef, _Lista, _Prodotti) => {
    console.log(moment().format("HH:mm:ss.SSS") + " - " + " GestioneListaObj - init");

    GestioneListaObj.objRef    = _objRef;
    GestioneListaObj.Lista     = _Lista;

    GestioneListaObj.toolbar = $('#toolbar').dxToolbar({
      items: [
        {
          location: 'before',
          widget: 'dxButton',
          options: {
            icon: 'fa-solid fa-arrows-rotate',
            type: 'default',
            text: 'Aggiorna',
            hint: "Aggiorna Lista Ordini",
            onClick() {
              //console.log(moment().format("HH:mm:ss.SSS") + " - " + " GestioneListaObj - toolbar - OnClick - Aggiorna");
              GestioneListaObj.btnRefresh();
            },
          },
        },
        {
          location: 'before',
          locateInMenu: 'never',
          template() {
            return $("<div class='ps-3 toolbar-label'><b>Evasione ordine:</b></div>");
          },
        },
        { // #EvasioneParziale
          location: 'before',
          locateInMenu: 'never',
          widget: 'dxSwitch',
          options: {
            elementAttr: { id: 'switchEvasioneParziale' },
            switchedOffText: 'Totale',
            switchedOnText: 'Parziale',
            height: 36,
            width: 108,
            onInitialized: function (e) {
              e.component.option("value", GestioneListaObj.EvasioneParziale);
              //e.component.toggleClass("myColor");
            },
            onValueChanged: function (e) {
              GestioneListaObj.EvasioneParziale = e.value;
            },
          },
        },
        {
          location: 'center',
          template() {
            return $("<div class='header-label'><b>" + GestioneListaObj.Lista.lista + "</b></div>");
          },
        },
        {
          location: 'after',
          locateInMenu: 'never',
          template() {
            return $("<div class='toolbar-label'><b>Conteggio:</b></div>");
          },
        },
        { // #TotaliToggle
          location: 'after',
          locateInMenu: 'never',
          widget: 'dxSwitch',
          options: {
            elementAttr: { id: 'switchConteggio' },
            switchedOffText: 'Attivi',
            switchedOnText: 'In Coda',
            height: 36,
            width: 108,
            onInitialized: function (e) {
              e.component.option("value", true);
            },
            onValueChanged: function (e) {
              if (e.value) {
                GestioneListaObj.CSS_RigaTotali = 1;
              } else {
                GestioneListaObj.CSS_RigaTotali = 0;
              }
              GestioneListaObj.restartUpdateHeaderTimer(40);
            },
          },
        },
        {
          location: 'after',
          locateInMenu: 'never',
          template() {
            return $("<div class='toolbar-label ps-3'><b>Visualizzazione:</b></div>");
          },
        },
        { // #FilterToggle
          location: 'after',
          widget: 'dxSwitch',
          locateInMenu: 'auto',
          options: {
            elementAttr: { id: 'switchVisualizzazione' },
            switchedOffText: 'Attivi',
            switchedOnText: 'Evasi',
            height: 36,
            width: 108,
            onInitialized: function (e) {
              e.component.option("value", GestioneListaObj.Filtro_StatoRiga);
            },
            onValueChanged: function (e) {
              //console.log(moment().format("HH:mm:ss.SSS") + " - " + " GestioneListaObj - toolbar - onValueChanged - switchVisualizzazione");
              GestioneListaObj.Filtro_StatoRiga = e.value;
              GestioneListaObj.btnRefresh();
            },
          },
        },
      ],
    }).dxToolbar("instance");

    GestioneListaObj.formEditOrdine = $("<div id ='formContainer'>").dxForm({
      readOnly: false,
      colCount: 3,
      items: [
        { dataField: "idOrdine", editorOptions: { disabled: true, }, },
        { dataField: "numeroCoperti", editorOptions: { disabled: true, }, },
        {
          dataField: "referente",
          editorType: 'dxTextBox',
          label: { text: "Referente al tavolo" },
          editorOptions: {
            onChange: function (e) {
              GestioneListaObj.OrdineToBeUpdated = true;
            }
          }
        },
        {
          dataField: "tavolo",
          editorType: 'dxTextBox',
          editorOptions: {
            elementAttr: {
              style: "font-size:18px; font-weight:bold",
            },
            onChange: function (e) {
              GestioneListaObj.OrdineToBeUpdated = true;
            }
          }
        },
        {
          dataField: "noteOrdine",
          editorType: "dxTextArea",
          colSpan: 2,
          editorOptions: {
            height: 112,
            elementAttr: {
              style: "font-size:18px; font-weight:bold",
            },
            onChange: function (e) {
              GestioneListaObj.OrdineToBeUpdated = true;
            },
          },
        },
      ],
    });

    GestioneListaObj.popupEditOrdine = $("#myEditOrdinePopup").dxPopup({
      visible: false,
      title: 'Test',
      width: 960,
      height: 640,
      position: {
        my: 'center',
        at: 'center',
        of: window
      },
      dragEnabled: true,
      contentTemplate: function (e) {
        const scrollView = $('<div id="textBlock" />').html("");
        e.append(GestioneListaObj.formEditOrdine);
        e.append(scrollView);
      },
      toolbarItems: [
        {
          widget: 'dxButton',
          toolbar: 'top',
          location: 'right',
          //visible: false,
          options: {
            icon: "fa-solid fa-lock-open",
            text: 'Sblocca liste',
            type: 'danger',
            onInitialized: function (e) {
              GestioneListaObj.btnSbloccaListe = e.component;
            },
            onClick() {
              //DevExpress.ui.notify('Sblocca button has been clicked!', "success");
              var result = DevExpress.ui.dialog.confirm("<i>Confermi lo sblocco delle liste di distribuzione in attesa?</i>", "Richiesta Conferma");
              result.done(function (dialogResult) {
                if (dialogResult) {
                  _objRef.invokeMethodAsync("SbloccaListeAsync", GestioneListaObj.Ordine.idOrdine)
                    .catch(err => console.error(err.toString()));
                  GestioneListaObj.popupEditOrdine.hide();
                }
              });
            },
          },
        },
        {
          widget: 'dxButton',
          toolbar: 'top',
          location: 'right',
          options: {
            icon: 'fa-solid fa-floppy-disk',
            text: 'Salva',
            type: 'success',
            onClick() {
              //DevExpress.ui.notify('Salva button has been clicked!', "success");

              if (GestioneListaObj.OrdineToBeUpdated) {
                _objRef.invokeMethodAsync('AggiornaDatiOrdineAsync', GestioneListaObj.Ordine)
                  .then(_retvalue => {
                    //console.log(moment().format("HH:mm:ss.SSS") + " - " + " GestioneListaObj - popupEditOrdine - onClick - Salva");
                    GestioneListaObj.btnRefresh(); // TODO - Provare ad aggiornare nella grid solo il record modificato
                  })
                  .catch(err => console.error(err.toString()));
              }
              GestioneListaObj.popupEditOrdine.hide();
            },
          },
        }, {
          widget: 'dxButton',
          toolbar: 'top',
          location: 'right',
          options: {
            icon: "fa-solid fa-square-xmark",
            text: 'Annulla',
            type: 'normal',
            onClick() {
              //DevExpress.ui.notify('Annulla button has been clicked!', "success");
              GestioneListaObj.popupEditOrdine.hide();
            },
          },
        }],
      onShowing: function (e) {
        GestioneListaObj.btnSbloccaListe.option("visible", GestioneListaObj.Lista.ioSonoListaPadre && GestioneListaObj.Ordine.idStatoOrdine < 2);

        document.getElementById("textBlock").innerHTML = GestioneListaObj.Ordine.righeHTML;
        //GestioneListaObj.formEditOrdine.dxForm("instance").getEditor('tavolo').focus();
        GestioneListaObj.formEditOrdine.dxForm("instance").getEditor('noteOrdine').focus();
      },
    }).dxPopup("instance");

    $("#myGridHeader").dxDataGrid({
      dataSource: [],
      height: "100%",
      columns: [
        {
          dataField: 'idOrdine',
          visible: false
        }, {
          dataField: 'progressivoSerata',
          visible: false
        }, {
          caption: 'Gest. Ordine',
          width: 50,
        }, {
          caption: 'Cassa',
          dataField: 'cassa',
          width: 35,
        }, {
          caption: 'Tavolo',
          dataField: 'tavolo',
          width: 95,
        }, {
          caption: 'Coperti',
          dataField: 'numeroCoperti',
          width: 45,
        }, {
          caption: 'Data/Ora',
          dataField: 'dataOra',
          width: 75,
        }, {
          caption: 'Referente al tavolo',
          dataField: 'referente',
          width: 100,
        }, {
          caption: 'Note Ordine',
          dataField: 'noteOrdine',
          width: 40,
        }, {
          caption: '-',
          width: 30,
          name: "Bomba",
          visible: false,
        },
      ],
      noDataText: "",
      focusedRowEnabled: false,
      showRowLines: true,
      rowAlternationEnabled: true,
      columnAutoWidth: false,
      wordWrapEnabled: true,
      showBorders: true,
      hoverStateEnabled: true,
      paging: { enabled: false },
      scrolling: {
        mode: "standard"
      },
      sorting: {
        mode: "none" 
      },
      onInitialized(e) {
        GestioneListaObj.gridHeader = e.component;
      },
      onRowPrepared: function (e) {
        switch (e.rowType) {
          case "data":
            e.rowElement.css({ height: 40 });
            break;
        }
      },
    });

    $("#myGridOrdini").dxDataGrid({
      dataSource: [],
      showColumnHeaders: false,
      height: "100%",
      keyExpr: "idOrdine",
      repaintChangesOnly: true,
      noDataText: "...",
      focusedRowEnabled: false,
      rowAlternationEnabled: true,
      columnAutoWidth: false,
      wordWrapEnabled: false,
      showRowLines: true,
      showBorders: true,
      hoverStateEnabled: true,
      paging: {
        enabled: true,
        pageSize: 20
      },
      pager: {
        visible: 'auto',
        showInfo: true,
        showNavigationButtons: true,
      },
      preloadEnabled: true,
      sorting: { mode: "none" },
      editing: {
        refreshMode: 'repaint',
        mode: 'cell', 
        allowUpdating: true,
        allowAdding: false,
        allowDeleting: false,
      },
      columns: [
        {
          dataField: 'idOrdine',
          visible: false, allowEditing: false,
        }, {
          dataField: 'progressivoSerata',
          visible: false, allowEditing: false,
        }, 
        {
          caption: "",
          width: 50, allowEditing: false,
          cellTemplate: function (element, info) {
            let flag = (info.data.statoRighe[1] + info.data.statoRighe[2] + info.data.statoRighe[3]) > 0;

            //flag = false;
            //for (var i = 0; i < info.data.righe.length; i++) {
            //  if (info.data.righe[i]) {
            //    if (info.data.righe[i].idStatoRiga > 0) {
            //      flag = true;
            //      break;
            //    }
            //  }
            //}
            // Se nessuna riga dell'ordine è stata ancora presa in carico
            if (!flag) {
              info.value = "<div class='PrendiInCaricoOrdine fa-solid fa-arrow-right-from-bracket'></div>";

              element[0].classList.remove("Class_EvadiLista");
              element[0].classList.add("Class_PrendiInCaricoOrdine");
            }
            else
            {
              flag = (info.data.statoRighe[0] + info.data.statoRighe[1] + info.data.statoRighe[3]) > 0;
              //flag = false;
              //for (var i = 0; i < info.data.righe.length; i++) {
              //  if (info.data.righe[i]) {
              //    if ((info.data.righe[i].idStatoRiga < 2) || (info.data.righe[i].idStatoRiga >= 3)) {
              //      flag = true;
              //      break;
              //    }
              //  }
              //}
              element[0].classList.remove("Class_PrendiInCaricoOrdine");
              if (flag) {
                info.value = "<div style='visibility:hidden'></div>";
              } else {
                info.value = "<div class='EvadiOrdine fa-solid fa-arrow-left'></div>";
                element[0].classList.add("Class_EvadiLista");
              }
            }
            $(info.value).appendTo(element);
          },
        },
        {
          caption: "",
          dataField: 'cassa',
          alignment: 'center',
          width: 35, allowEditing: false,
          cellTemplate: function (element, info) {
            $("<div style='font-size:13px; font-weight:bold'>" + info.value + "</div>").appendTo(element);
          },
        }, {
          caption: "",
          dataField: 'tavolo',
          alignment: 'center',
          dataType: "string",
          width: 95,
          cellTemplate: function (element, info) {
            if (info.value == 'BANCO') {
              $("<div style='font-size:14px; font-weight:bold'>" + info.value + "</div>").appendTo(element);
            } else {
              $("<div style='font-size:18px; font-weight:bold'>" + info.value + "</div>").appendTo(element);
            }
          },
        }, {
          caption: "",
          dataField: 'numeroCoperti',
          dataType: "string",
          alignment: 'center',
          width: 45,
          cellTemplate: function (element, info) {
            $("<div style='font-size:13px; font-weight:bold'>" + info.value + "</div>").appendTo(element);
          },
        }, {
          caption: "",
          dataField: 'dataOra',
          alignment: 'center',
          allowEditing: false,
          width: 75,
          cellTemplate: function (element, info) {
            let tipid = info.data.idOrdine + "_" + info.column.headerId;
            let sitetextid = tipid + "_" + info.column.index;

            if (info.data.idStatoOrdine == 99) {
              info.value = ""
            } else {
              $("<div id=" + tipid + "></div><div id=" + sitetextid + " style='font-size:13px; font-weight:bold'>" + info.value + "<div>").appendTo(element);
              $("#" + tipid).dxTooltip({
                target: "#" + sitetextid,
                showEvent: "dxhoverstart",
                hideEvent: "dxhoverend",
                contentTemplate: function (contentElement) {
                  contentElement.html(info.data.righeHTML);
                }
              });
            }
          }
        }, {
          caption: "",
          dataField: 'referente',
          width: 100,
          cellTemplate: function (element, info) {
            $("<div style='font-size:13px; font-weight:bold; padding-left: 6px;text-overflow: ellipsis; overflow: hidden;'>" + info.value + "</div>").appendTo(element);
          },
        }, {
          caption: "",
          dataField: 'noteOrdine',
          alignment: 'center',
          width: 40,
          allowEditing: false,
          cellTemplate: function (element, info) {
            let tipid = info.data.idOrdine + "_" + info.column.headerId;
            let sitetextid = tipid + "_" + info.column.index;

            element[0].classList.add("Class_editCommento");

            let testoAsterisco = info.value.length > 0 ? "(*)" : "";

            if (GestioneListaObj.Lista.ioSonoListaPadre && info.data.idStatoOrdine < 2) {
              testoAsterisco = testoAsterisco + "<div style='position:relative'><div class='fa fa-lock child_top_right'></div></div>";
            }

            $("<div id=" + tipid + "></div><div id=" + sitetextid + " style='font-size:15px; font-weight:bold;text-align:center'>" + testoAsterisco + "<div>").appendTo(element);
            if (info.value.length > 0) {
              $("#" + tipid).dxTooltip({
                target: "#" + sitetextid,
                showEvent: "dxhoverstart",
                hideEvent: "dxhoverend",
                contentTemplate: function (contentElement) {
                  contentElement.html("<b>" + info.value.replace(/(?:\r\n|\r|\n)/g, '<br>') + "</b>");
                }
              });
            }
          },
        }, {
          caption: "",
          width: 30,
          name: "Bomba",
          visible: false,
          alignment: 'center',
          allowEditing: false,
          cellTemplate: function (element, info) {
            // Mi basta un Prodotto In giallo per mostrare la Bomba
            let flagBombaAttiva = (info.data.statoRighe[1] > 0);
            //for (var i = 0; i < info.data.righe.length; i++) {
            //  if (info.data.righe[i]) {
            //    if ((info.data.righe[i].idStatoRiga > 0) && (info.data.righe[i].idStatoRiga < 2)) {
            //      flagBombaAttiva = true;
            //      break;
            //    }
            //  }
            //}
            if (flagBombaAttiva) {
              info.value = "<div title='Evadi tutti i prodotti della lista' class='fa-solid fa-bomb' style='font-size: 20px; color: green;'></div>";
            } else {
              info.value = "<div class='fa-solid fa-bomb' style='font-size: 20px; color: transparent;'></div>";
            }

            $(info.value).appendTo(element);
          }
        },
      ],
      onInitialized(e) {
        GestioneListaObj.gridOrdini = e.component;
      },
      onEditingStart(e) {
        GestioneListaObj.Ordine = e.data;
        GestioneListaObj.editedColumn = e.column;
        //console.log(moment().format("HH:mm:ss.SSS") + " - " + " GestioneListaObj - EditingStart - " + e.column.dataField);
      },
      onRowUpdating(e) {
        //console.log(moment().format("HH:mm:ss.SSS") + " - " + " GestioneListaObj - RowUpdating");
      },
      onRowUpdated(e) {
        //console.log(moment().format("HH:mm:ss.SSS") + " - " + " GestioneListaObj - RowUpdated");
      },
      onSaving(e) {
        //console.log(moment().format("HH:mm:ss.SSS") + " - " + " GestioneListaObj - Saving");
      },
      onSaved(e) {
        switch (GestioneListaObj.editedColumn.dataField) {
          case "tavolo":
            _objRef.invokeMethodAsync('AggiornaDatiOrdineAsync', e.changes[0].data).catch(err => console.error(err.toString()));
            break;

          case "numeroCoperti":
            _objRef.invokeMethodAsync('AggiornaDatiOrdineAsync', e.changes[0].data).catch(err => console.error(err.toString()));
            break;

          case "referente":
            _objRef.invokeMethodAsync('AggiornaDatiOrdineAsync', e.changes[0].data).catch(err => console.error(err.toString()));
            break;

          default:
            break;
        }
        //console.log(moment().format("HH:mm:ss.SSS") + " - " + " GestioneListaObj - Saved - " + GestioneListaObj.editedColumn.dataField);
      },
      onEditCanceling(e) {
        //console.log(moment().format("HH:mm:ss.SSS") + " - " + " GestioneListaObj - EditCanceling");
      },
      onEditCanceled(e) {
        //console.log(moment().format("HH:mm:ss.SSS") + " - " + " GestioneListaObj - EditCanceled");
      },
      onRowPrepared: function (e) {
        switch (e.rowType) {
          case "header":
            //e.rowElement.css({ height: 40 });
            break;
          case "data":
            e.rowElement.css({ height: 33 });
            break;
        }
      },
      onCellClick: function (e) {
        if (e.rowType == 'data' && e.column.name == "Bomba") {
          let flagBombaAttiva = (e.data.statoRighe[1] > 0);
          if (flagBombaAttiva) {
            _objRef.invokeMethodAsync('AggiornaStatoListaAsync', GestioneListaObj.Filtro_StatoRiga, e.data.idOrdine)
              .then(_retvalue => {
                var retvalue = JSON.parse(_retvalue);

                if ("righe" in retvalue) {
                  var dati = retvalue["righe"];
                  e.data.righe = dati;
                }

                if ("stato" in retvalue) {
                  var dati = retvalue["stato"];
                  e.data.statoRighe = dati;
                }
                e.component.repaintRows(e.rowIndex);
              }
              ).catch(err => console.error(err.toString()));
            GestioneListaObj.restartUpdateHeaderTimer(40);
          }
        } else {
          if (e.cellElement.hasClass('Class_changedSTATUS')) {
            let _PrimaColonnaProdotti = GestioneListaObj.gridHeader.option("columns").length;

            var i = e.column.index - _PrimaColonnaProdotti;

            if (e.data.righe[i]) {
              let Riga = e.data.righe[i];

              let QuantitàEvasaOld = Riga.quantitàEvasa;
              let QuantitàEvasaNew = Riga.quantitàEvasa;

              if ((GestioneListaObj.EvasioneParziale == true) && (Riga.idStatoRiga == 1)) {
                QuantitàEvasaNew += 1;
                if (QuantitàEvasaNew > Riga.quantitàProdotto)
                  QuantitàEvasaNew = 0;
              } else {
                var IdStatoRiga_Nuovo;
                switch (Riga.idStatoRiga) {
                  case -1:
                  case 0: IdStatoRiga_Nuovo = 1; break;
                  case 1: IdStatoRiga_Nuovo = 2; break;
                  case 2: IdStatoRiga_Nuovo = 1; break;
                  default: IdStatoRiga_Nuovo = 0; break;
                }
                if (IdStatoRiga_Nuovo != 0) {
                  _objRef.invokeMethodAsync('AggiornaStatoRigaAsync', GestioneListaObj.Filtro_StatoRiga, Riga, IdStatoRiga_Nuovo)
                    .then(_retvalue => {
                      var retvalue = JSON.parse(_retvalue);

                      if ("righe" in retvalue) {
                        var dati = retvalue["righe"];
                        e.data.righe = dati;
                      }

                      if ("stato" in retvalue) {
                        var dati = retvalue["stato"];
                        e.data.statoRighe = dati;
                      }
                      e.component.repaintRows(e.rowIndex);
                    }
                  ).catch(err => console.error(err.toString()));
                  GestioneListaObj.restartUpdateHeaderTimer(40);
                }
              }

              if (QuantitàEvasaOld != QuantitàEvasaNew) {
                _objRef.invokeMethodAsync('AggiornaQuantitàEvasaAsync', GestioneListaObj.Filtro_StatoRiga, Riga, QuantitàEvasaNew)
                  .then(_retvalue => {
                    var retvalue = JSON.parse(_retvalue);

                    if ("righe" in retvalue) {
                      var dati = retvalue["righe"];
                      e.data.righe = dati;
                    }

                    if ("stato" in retvalue) {
                      var dati = retvalue["stato"];
                      e.data.statoRighe = dati;
                    }
                    e.component.repaintRows(e.rowIndex);
                    //console.log(moment().format("HH:mm:ss.SSS") + " - " + " GestioneListaObj - AggiornaQuantitàEvasaAsync");
                  }
                  ).catch(err => console.error(err.toString()));

                if (QuantitàEvasaNew == Riga.quantitàProdotto) {
                  IdStatoRiga_Nuovo = 2;
                  _objRef.invokeMethodAsync('AggiornaStatoRigaAsync', GestioneListaObj.Filtro_StatoRiga, Riga, IdStatoRiga_Nuovo)
                    .then(_retvalue => {
                      var retvalue = JSON.parse(_retvalue);

                      if ("righe" in retvalue) {
                        var dati = retvalue["righe"];
                        e.data.righe = dati;
                      }

                      if ("stato" in retvalue) {
                        var dati = retvalue["stato"];
                        e.data.statoRighe = dati;
                      }
                      e.component.repaintRows(e.rowIndex);
                    }
                  ).catch(err => console.error(err.toString()));
                }
                GestioneListaObj.restartUpdateHeaderTimer(40);
              }
            }
          }

          if (e.cellElement.hasClass('Class_editCommento')) { //         if (o.column.cls == 'editCommento') {
            GestioneListaObj.Ordine = e.data;
            GestioneListaObj.OrdineToBeUpdated = false;

            GestioneListaObj.popupEditOrdine.option("title", "Dati Ordine #" + e.data.progressivoSerata + " (" + e.data.idOrdine + ") delle ore " + e.data.dataOra + " - Cassa: " + e.data.cassa );
            GestioneListaObj.formEditOrdine.dxForm("instance").option("formData", e.data);
            GestioneListaObj.popupEditOrdine.show();
          }

          if (e.cellElement.hasClass('Class_PrendiInCaricoOrdine')) {
            _objRef.invokeMethodAsync('PrendiInCaricoOrdineAsync',
              GestioneListaObj.Filtro_StatoRiga,
              e.data.idOrdine).then(_retvalue => {
                var retvalue = JSON.parse(_retvalue);

                if ("righe" in retvalue) {
                  var dati = retvalue["righe"];
                  e.data.righe = dati;
                }

                if ("stato" in retvalue) {
                  var dati = retvalue["stato"];
                  e.data.statoRighe = dati;
                }
                e.component.repaintRows(e.rowIndex);

                GestioneListaObj.restartUpdateHeaderTimer(40);
              }
            ).catch(err => console.error(err.toString()));
          }

          if (e.cellElement.hasClass('Class_EvadiLista')) {
            //DevExpress.ui.notify('Evasione Ordine #' + e.data.idOrdine);
            _objRef.invokeMethodAsync('EvadiListaAsync',
              e.data.idOrdine).then(_retvalue => {
            //    var retvalue = JSON.parse(_retvalue);
            //    listRefreshEvadiOrdine();
                // console.log(moment().format("HH:mm:ss.SSS") + " - " + " GestioneListaObj - EvadiListaAsync");
                GestioneListaObj.btnRefresh(); // TODO - Provare ad aggiornare nella grid solo il record modificato
                //GestioneListaObj.restartUpdateHeaderTimer(100);
              }
            ).catch(err => console.error(err.toString()));
          }
        }
      },
    });

    GestioneListaObj.gestisciColonne(_Prodotti);

    resyncSelectBox(_Lista.idLista);
  },

  gestisciColonne: (_Prodotti) => {
    GestioneListaObj.gridHeader.beginUpdate();
    GestioneListaObj.gridOrdini.beginUpdate();

    GestioneListaObj.gridHeader.columnOption("Bomba", "visible", (_Prodotti.length > 5)); // Era 12
    GestioneListaObj.gridOrdini.columnOption("Bomba", "visible", (_Prodotti.length > 5));

    let _PrimaColonnaProdotti = GestioneListaObj.gridHeader.option("columns").length;
    for (let i = _PrimaColonnaProdotti; GestioneListaObj.gridHeader.columnCount() - _PrimaColonnaProdotti; i++) {
      let colId = 'Colonna_' + (i - _PrimaColonnaProdotti);

      GestioneListaObj.gridHeader.deleteColumn(colId);
      GestioneListaObj.gridOrdini.deleteColumn(colId);
    }

    let sFontSize = "10px"; // Era 11px
    let iWidth = 57;        // Era 64

    const numProdotti = _Prodotti.length;
    switch (true) {
      case (numProdotti <= 6):
        sFontSize = "14px";
        iWidth = 120;
        break;

      case (numProdotti <= 8):
        sFontSize = "14px";
        iWidth = 95;
        break;

      case (numProdotti <= 10):
        sFontSize = "13px";
        iWidth = 85;
        break;

      case (numProdotti <= 12):
        sFontSize = "12px";
        iWidth = 75;
        break;
    }

    // console.log(moment().format("HH:mm:ss.SSS") + " - " + " GestioneListaObj - gestisciColonne - Prodotti: " + _Prodotti.length.toString());
    for (var i = 0; i < _Prodotti.length; i++) {
      GestioneListaObj.gridHeader.addColumn(
        {
          caption: _Prodotti[i].nomeProdotto,
          headerCellTemplate: function (header, info) {
            $("<div style='font-size:" + sFontSize + ";'>")
              .html(info.column.caption.replaceAll("<CR>", "<br>"),)
              .appendTo(header);
          },
          dataField: 'Colonna_' + i, width: iWidth,
          cellTemplate: function (element, info) {
            if (info.value != " ") {
              element.append($("<div class='status_" + GestioneListaObj.CSS_RigaTotali + "'>" + info.value + "</div>"))
            } else {
              element.append($("<div class='status_" + GestioneListaObj.CSS_RigaTotali + "'>&nbsp</div>"))
            }
          }
        });
      GestioneListaObj.gridOrdini.addColumn({
        dataField: 'Colonna_' + i,
        headerCellTemplate: $('<div></div>'),
        width: iWidth,
        allowEditing: false,
        alignment: "center",
        cellTemplate: function (element, info) {
          var idx = info.column.index - _PrimaColonnaProdotti;
          if (info.data.righe[idx]) {
            info.value = info.data.righe[idx].quantitàProdotto;
            element[0].classList.add("Class_changedSTATUS");
            switch (info.data.righe[idx].idStatoRiga) {
              case -1:
                info.value = "<div style='font-size:20px; font-weight:bold; background-color:#800000; color:#FFFFFF'>«" + info.value + "»</div>"
                break;

              case 0:
                info.value = "<div style='font-size:20px; font-weight:bold; background-color:#dc3545; color:#000000'>" + info.value + "</div>";
                break;

              case 1:
                info.value = "<div style='font-size:20px; font-weight:bold; background-color:#ffc107; color:#000000'>" + info.value + "<div class='badge badge-success count-notif'>" + info.data.righe[idx].quantitàEvasa + "</div></div>"
                break;

              case 2:
                info.value = "<div style='font-size:20px; font-weight:bold; background-color:#28a745; color:#000000'>" + info.value + "</div>";
                break;

              case 3:
                info.value = "<div style='font-size:20px; font-weight:bold; background-color:#FFFFFF; color:#000000; border-style: solid; border-width:2px;border-color: #008000'>" + info.value + "</div>"
                break;

              case 99: // Per la gestione della grid vuota
                info.value = ""
                break;

              default:
                info.value = "<div style='font-size:20px; font-weight:bold; background-color:#FF00FF; color:#000000'>" + info.value + "</div> "
                break;
            }
          } else {
            info.value = "";
            info.value = "<div style='font-size:20px; font-weight:bold; background-color:#FF00FF; color:#000000'>" + info.value + "</div>"
            element[0].classList.remove("Class_changedSTATUS");
          }
          element.append(info.value);
        }
      });
    }
    GestioneListaObj.gridHeader.endUpdate();
    GestioneListaObj.gridOrdini.endUpdate();

    //console.log(moment().format("HH:mm:ss.SSS") + " - " + " GestioneListaObj - gestisciColonne - Prodotti: " + _Prodotti.length.toString()); 
    GestioneListaObj.btnRefresh();
  },

  btnRefresh: () =>  {
    //console.log(moment().format("HH:mm:ss.SSS") + " - " + " GestioneListaObj - btnRefresh - Enter");

    //GestioneListaObj.startTime = performance.now();
    GestioneListaObj.objRef.invokeMethodAsync("RefreshGridOrdiniRowsAsync", GestioneListaObj.Filtro_StatoRiga)
      .then(_retvalue => {
        GestioneListaObj.listRefreshAll(_retvalue);

        //GestioneListaObj.endTime = performance.now();
        //console.log(moment().format("HH:mm:ss.SSS") + " - " + " GestioneListaObj - btnRefresh - Exit - " + (GestioneListaObj.endTime - GestioneListaObj.startTime).toFixed(0) + ' msec');
      });
    GestioneListaObj.restartUpdateHeaderTimer(40);
  },

  onNotifyStatoOrdine: (_idOrdine) => {
    // TODO
    console.log(moment().format("HH:mm:ss.SSS") + " - " + " GestioneListaObj - onNotifyStatoOrdine");
  },

  onNotifyStatoLista: () => {
    console.log(moment().format("HH:mm:ss.SSS") + " - " + " GestioneListaObj - onNotifyStatoLista");

    GestioneListaObj.objRef.invokeMethodAsync("RefreshGridOrdiniRowsAsync", GestioneListaObj.Filtro_StatoRiga)
      .then(_retvalue => {
        GestioneListaObj.listRefreshAll(_retvalue);
      });
    GestioneListaObj.restartUpdateHeaderTimer(40);
  },

  listRefreshAll: (dati)  => {
    if (GestioneListaObj.gridOrdini != null) {
      GestioneListaObj.gridOrdini.beginUpdate();
      GestioneListaObj.gridOrdini.option("dataSource", dati);
      GestioneListaObj.gridOrdini.endUpdate();
      GestioneListaObj.gridOrdini.refresh();
    }
  },

  restartUpdateHeaderTimer: (_ticks) => {
    clearTimeout(GestioneListaObj.idTimerUpdateRequest);
    GestioneListaObj.idTimerUpdateRequest = setTimeout(function ()
    {
      //console.log(moment().format("HH:mm:ss.SSS") + " -  GestioneListaObj - RefreshGridOrdiniHeaderAsync - updateHeaderRequest"); // + GestioneListaObj.idTimerUpdateRequest);

      GestioneListaObj.startTime = performance.now();
      clearTimeout(GestioneListaObj.idTimerUpdateRequest);

      GestioneListaObj.objRef.invokeMethodAsync('RefreshGridOrdiniHeaderAsync')
        .then(_RigaTotali => {
          if (GestioneListaObj.gridHeader != null) {
            //console.log(moment().format("HH:mm:ss.SSS") + " -  GestioneListaObj - RefreshGridOrdiniHeaderAsync - " + (performance.now() - GestioneListaObj.startTime).toFixed(0) + ' msec');
            let RigaTotali = JSON.parse(_RigaTotali);

            //console.log(moment().format("HH:mm:ss.SSS") + " -  GestioneListaObj - RefreshGridOrdiniHeaderAsync - " + (performance.now() - GestioneListaObj.startTime).toFixed(0) + ' msec');
            let _CSS_RigaTotali = GestioneListaObj.CSS_RigaTotali;

            let strRiga = "";
            strRiga = strRiga + "[{";
            for (var i = 0; i < RigaTotali[_CSS_RigaTotali].length; i++) {
              //let colId = 'Colonna_' + i;

              if (RigaTotali[_CSS_RigaTotali][i] == 0)
                strRiga = strRiga + '"' + ('Colonna_' + i) + '": " ",'
              else
                strRiga = strRiga + '"' + ('Colonna_' + i) + '": "' + RigaTotali[_CSS_RigaTotali][i] + '",'
            }
            strRiga = strRiga.slice(0, -1) + "}]";

            GestioneListaObj.gridHeader.beginUpdate();
            GestioneListaObj.gridHeader.option("dataSource", JSON.parse(strRiga));
            GestioneListaObj.gridHeader.endUpdate();
            GestioneListaObj.gridHeader.refresh();
          }
        }
      );
    }, _ticks);
  },

  dispose: () => {
    try {
      resyncSelectBox(null);

      $("#toolbar").dxToolbar("dispose");
      //$("#toolbar").remove();

      $("#myGridHeader").dxDataGrid("dispose");
      //$("#myGridHeader").remove();

      $("#myGridOrdini").dxDataGrid("dispose");
      //$("#myGridOrdini").remove();

      //console.log(moment().format("HH:mm:ss.SSS") + " - " + " GestioneListaObj - dispose");
    } catch (error) {
      //console.error(moment().format("HH:mm:ss.SSS") + " - " + " GestioneListaObj - dispose - Error: " + error);
    }
  },
}
